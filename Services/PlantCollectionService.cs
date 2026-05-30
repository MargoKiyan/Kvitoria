using Kvitoria.Data;
using Kvitoria.Extensions;
using Kvitoria.Models;
using Kvitoria.Models.Enums;
using Kvitoria.Services.Auth;
using Kvitoria.Services.Images;
using Kvitoria.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Kvitoria.Services;

public class PlantCollectionService(
    KvitoriaDbContext dbContext,
    IUserContext userContext,
    IPlantImageService plantImageService) : IPlantCollectionService
{
    public async Task<DashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var plants = await VisiblePlants()
            .AsNoTracking()
            .OrderBy(plant => plant.Name)
            .ToListAsync(cancellationToken);

        var plantIds = plants.Select(plant => plant.Id).ToArray();
        var recentLogs = await dbContext.CareLogs
            .AsNoTracking()
            .Include(log => log.Plant)
            .Where(log => plantIds.Contains(log.PlantId))
            .OrderByDescending(log => log.PerformedOn)
            .ThenByDescending(log => log.Id)
            .Take(6)
            .ToListAsync(cancellationToken);

        var activePlants = plants.Where(plant => plant.Status != PlantStatus.Archived).ToList();
        var allPlantsNeedingCare = activePlants
            .Where(plant => plant.NeedsAttention(today))
            .OrderBy(plant => plant.NextWateringDate ?? DateOnly.MaxValue)
            .ThenBy(plant => plant.Name)
            .ToList();

        return new DashboardViewModel
        {
            TotalPlants = plants.Count,
            NeedsAttentionCount = allPlantsNeedingCare.Count,
            DueTodayCount = activePlants.Count(plant => plant.NextWateringDate.HasValue
                && plant.NextWateringDate.Value <= today),
            ArchivedCount = plants.Count(plant => plant.Status == PlantStatus.Archived),
            PlantsNeedingCare = allPlantsNeedingCare.Take(5).ToList(),
            NewestPlants = plants
                .OrderByDescending(plant => plant.AcquisitionDate ?? DateOnly.MinValue)
                .ThenByDescending(plant => plant.Id)
                .Take(4)
                .ToList(),
            RecentCareLogs = recentLogs
        };
    }

    public async Task<PlantListViewModel> GetPlantsAsync(
        string? search,
        PlantStatus? status,
        int page,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var query = VisiblePlants().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(plant =>
                EF.Functions.ILike(plant.Name, pattern)
                || EF.Functions.ILike(plant.Species, pattern)
                || plant.Variety != null && EF.Functions.ILike(plant.Variety, pattern)
                || plant.Location != null && EF.Functions.ILike(plant.Location, pattern));
        }

        if (status.HasValue)
        {
            query = ApplyStatusFilter(query, status.Value, today);
        }

        var filteredCount = await query.CountAsync(cancellationToken);
        var pagination = PaginationViewModel.Create(
            page,
            PaginationViewModel.DefaultPageSize,
            filteredCount,
            "Index",
            new Dictionary<string, string?>
            {
                ["search"] = search,
                ["status"] = status?.ToString()
            });

        var plants = await query
            .OrderBy(plant => plant.NextWateringDate ?? DateOnly.MaxValue)
            .ThenBy(plant => plant.Name)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        var allPlants = await VisiblePlants().AsNoTracking().ToListAsync(cancellationToken);
        var options = EnumSelectListExtensions.ToSelectList<PlantStatus>().ToList();
        options.Insert(0, new SelectListItem("Усі статуси", string.Empty));

        return new PlantListViewModel
        {
            Search = search,
            Status = status,
            Plants = plants,
            FilteredCount = filteredCount,
            TotalCount = allPlants.Count,
            NeedsAttentionCount = allPlants.Count(plant => plant.NeedsAttention(today)),
            StatusOptions = options,
            Pagination = pagination
        };
    }

    public Task<Plant?> GetPlantAsync(int id, CancellationToken cancellationToken = default)
    {
        return VisiblePlants()
            .AsNoTracking()
            .Include(plant => plant.CareLogs.OrderByDescending(log => log.PerformedOn).ThenByDescending(log => log.Id))
            .FirstOrDefaultAsync(plant => plant.Id == id, cancellationToken);
    }

    public async Task<int> CreatePlantAsync(PlantFormViewModel model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userContext.UserId))
        {
            throw new InvalidOperationException("Потрібно увійти в систему, щоб додати рослину.");
        }

        var speciesName = await dbContext.PlantSpecies
            .Where(species => species.Id == model.PlantSpeciesId)
            .Select(species => species.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(speciesName))
        {
            throw new InvalidOperationException("Оберіть вид рослини з каталогу.");
        }

        var plant = new Plant(
            model.Name,
            speciesName,
            model.Status,
            model.LightRequirement,
            model.WateringFrequency);

        plant.AssignOwner(userContext.UserId);
        await ApplyPlantFormAsync(plant, model, cancellationToken);

        dbContext.Plants.Add(plant);
        await dbContext.SaveChangesAsync(cancellationToken);

        return plant.Id;
    }

    public async Task<bool> UpdatePlantAsync(
        int id,
        PlantFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        var plant = await VisiblePlants()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (plant is null)
        {
            return false;
        }

        await ApplyPlantFormAsync(plant, model, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeletePlantAsync(int id, CancellationToken cancellationToken = default)
    {
        var plant = await VisiblePlants()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (plant is null)
        {
            return false;
        }

        dbContext.Plants.Remove(plant);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<OperationResult> AddCareLogAsync(
        CareLogFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        var plant = await VisiblePlants()
            .Include(item => item.CareLogs)
            .FirstOrDefaultAsync(item => item.Id == model.PlantId, cancellationToken);

        if (plant is null)
        {
            return OperationResult.Failure("Рослину не знайдено або немає доступу.");
        }

        var alreadyWatered = model.ActivityType == CareActivityType.Watering
            && await dbContext.CareLogs.AnyAsync(log =>
                log.PlantId == model.PlantId
                && log.ActivityType == CareActivityType.Watering
                && log.PerformedOn == model.PerformedOn,
                cancellationToken);

        if (alreadyWatered)
        {
            return OperationResult.Failure("Полив для цієї рослини вже занотовано на обрану дату.");
        }

        plant.RegisterCare(model.ActivityType, model.PerformedOn, model.Notes);
        await dbContext.SaveChangesAsync(cancellationToken);

        return OperationResult.Success(model.ActivityType == CareActivityType.Watering
            ? "Полив записано, дату наступного поливу оновлено."
            : "Запис догляду додано.");
    }

    public Task<CareLog?> GetCareLogAsync(int id, CancellationToken cancellationToken = default)
    {
        return dbContext.CareLogs
            .AsNoTracking()
            .Include(log => log.Plant)
            .Where(log => userContext.IsAdmin
                || log.Plant != null && log.Plant.UserId == userContext.UserId)
            .FirstOrDefaultAsync(log => log.Id == id, cancellationToken);
    }

    public async Task<bool> DeleteCareLogAsync(int id, CancellationToken cancellationToken = default)
    {
        var careLog = await dbContext.CareLogs
            .Include(log => log.Plant)
            .Where(log => userContext.IsAdmin
                || log.Plant != null && log.Plant.UserId == userContext.UserId)
            .FirstOrDefaultAsync(log => log.Id == id, cancellationToken);

        if (careLog is null)
        {
            return false;
        }

        dbContext.CareLogs.Remove(careLog);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private IQueryable<Plant> VisiblePlants()
    {
        var query = dbContext.Plants.AsQueryable();

        if (userContext.IsAdmin)
        {
            return query;
        }

        return string.IsNullOrWhiteSpace(userContext.UserId)
            ? query.Where(plant => false)
            : query.Where(plant => plant.UserId == userContext.UserId);
    }

    private static IQueryable<Plant> ApplyStatusFilter(
        IQueryable<Plant> query,
        PlantStatus status,
        DateOnly today)
    {
        if (status == PlantStatus.NeedsCare)
        {
            return query.Where(plant =>
                plant.Status == PlantStatus.NeedsCare
                || plant.Status != PlantStatus.Archived
                    && plant.NextWateringDate.HasValue
                    && plant.NextWateringDate.Value <= today);
        }

        if (status == PlantStatus.Archived)
        {
            return query.Where(plant => plant.Status == PlantStatus.Archived);
        }

        return query.Where(plant =>
            plant.Status == status
            && (!plant.NextWateringDate.HasValue || plant.NextWateringDate.Value > today));
    }

    private async Task ApplyPlantFormAsync(
        Plant plant,
        PlantFormViewModel model,
        CancellationToken cancellationToken)
    {
        var species = await dbContext.PlantSpecies
            .FirstOrDefaultAsync(item => item.Id == model.PlantSpeciesId, cancellationToken);

        if (species is null)
        {
            throw new InvalidOperationException("Оберіть вид рослини з каталогу.");
        }

        var variety = model.PlantVarietyId.HasValue
            ? await dbContext.PlantVarieties
                .FirstOrDefaultAsync(item => item.Id == model.PlantVarietyId.Value, cancellationToken)
            : null;

        if (model.PlantVarietyId.HasValue && variety is null)
        {
            throw new InvalidOperationException("Оберіть форму або сорт з каталогу.");
        }

        if (variety is not null && variety.PlantSpeciesId != species.Id)
        {
            throw new InvalidOperationException("Форма або сорт не належить до обраного виду рослини.");
        }

        var imagePath = await plantImageService.SaveImageAsync(model.Photo, model.ImageUrl, cancellationToken);

        plant.UpdateProfile(
            model.Name,
            species.Name,
            variety?.Name,
            model.Status,
            model.LightRequirement,
            model.WateringFrequency,
            model.Location,
            model.PotDiameterCm,
            model.AcquisitionDate,
            model.AcquisitionSource,
            imagePath,
            model.Notes);

        plant.AssignCatalog(species, variety);
        plant.UpdateCareSchedule(model.LastWateredDate);
    }
}
