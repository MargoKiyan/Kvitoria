using Kvitoria.Data;
using Kvitoria.Models.PlantCatalog;
using Kvitoria.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Kvitoria.Services.Admin;

public class AdminPlantCatalogService(KvitoriaDbContext dbContext) : IAdminPlantCatalogService
{
    public async Task<AdminPlantsViewModel> GetCatalogAsync(
        string? search,
        string sort,
        string variantFilter,
        int page,
        CancellationToken cancellationToken = default)
    {
        var speciesQuery = dbContext.PlantSpecies
            .AsNoTracking()
            .Include(species => species.Varieties)
            .AsQueryable();

        speciesQuery = ApplySearch(speciesQuery, search);
        speciesQuery = ApplyVariantFilter(speciesQuery, variantFilter);
        speciesQuery = ApplySorting(speciesQuery, sort);

        var filteredSpeciesCount = await speciesQuery.CountAsync(cancellationToken);
        var pagination = PaginationViewModel.Create(
            page,
            PaginationViewModel.DefaultPageSize,
            filteredSpeciesCount,
            "Plants",
            new Dictionary<string, string?>
            {
                ["search"] = search,
                ["sort"] = sort,
                ["variantFilter"] = variantFilter
            });

        var species = await speciesQuery
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);
        var speciesOptions = await dbContext.PlantSpecies
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
        var varieties = await dbContext.PlantVarieties
            .AsNoTracking()
            .Include(variety => variety.Species)
            .OrderBy(variety => variety.Species!.Name)
            .ThenBy(variety => variety.Name)
            .ToListAsync(cancellationToken);

        return new AdminPlantsViewModel
        {
            Search = search,
            Sort = sort,
            VariantFilter = variantFilter,
            SpeciesCount = await dbContext.PlantSpecies.CountAsync(cancellationToken),
            VarietyCount = await dbContext.PlantVarieties.CountAsync(cancellationToken),
            SpeciesOptions = speciesOptions,
            Species = species,
            Varieties = varieties,
            Pagination = pagination
        };
    }

    public async Task<AdminActionResult> AddSpeciesAsync(
        AdminPlantSpeciesFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = model.Name.Trim();
        var exists = await dbContext.PlantSpecies
            .AnyAsync(species => species.Name.ToLower() == normalizedName.ToLower(), cancellationToken);

        if (exists)
        {
            return AdminActionResult.Failure("Такий вид уже існує.");
        }

        dbContext.PlantSpecies.Add(new PlantSpecies(normalizedName));
        await dbContext.SaveChangesAsync(cancellationToken);

        return AdminActionResult.Success("Вид рослини додано.");
    }

    public async Task<AdminActionResult> UpdateSpeciesAsync(
        int id,
        AdminPlantSpeciesFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        var species = await dbContext.PlantSpecies.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (species is null)
        {
            return AdminActionResult.Missing();
        }

        var normalizedName = model.Name.Trim();
        var duplicateSpecies = await dbContext.PlantSpecies.AnyAsync(item =>
            item.Id != id && item.Name.ToLower() == normalizedName.ToLower(),
            cancellationToken);

        if (duplicateSpecies)
        {
            return AdminActionResult.Failure("Такий вид уже існує.");
        }

        species.Update(normalizedName);
        await dbContext.SaveChangesAsync(cancellationToken);

        return AdminActionResult.Success("Вид рослини оновлено.");
    }

    public async Task<AdminActionResult> DeleteSpeciesAsync(int id, CancellationToken cancellationToken = default)
    {
        var species = await dbContext.PlantSpecies
            .Include(item => item.Plants)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (species is null)
        {
            return AdminActionResult.Missing();
        }

        if (species.Plants.Count > 0)
        {
            return AdminActionResult.Failure("Вид використовується в колекціях користувачів, тому його не видалено.");
        }

        dbContext.PlantSpecies.Remove(species);
        await dbContext.SaveChangesAsync(cancellationToken);

        return AdminActionResult.Success("Вид рослини видалено.");
    }

    public async Task<AdminActionResult> AddVarietyAsync(
        AdminPlantVarietyFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        var speciesExists = await dbContext.PlantSpecies
            .AnyAsync(species => species.Id == model.PlantSpeciesId, cancellationToken);

        if (!speciesExists)
        {
            return AdminActionResult.Failure("Оберіть існуючий вид рослини.");
        }

        var normalizedName = model.Name.Trim();
        var exists = await dbContext.PlantVarieties.AnyAsync(variety =>
            variety.PlantSpeciesId == model.PlantSpeciesId
            && variety.Type == model.Type
            && variety.Name.ToLower() == normalizedName.ToLower(),
            cancellationToken);

        if (exists)
        {
            return AdminActionResult.Failure("Така форма або сорт уже існує для цього виду.");
        }

        dbContext.PlantVarieties.Add(new PlantVariety(model.PlantSpeciesId, normalizedName, model.Type));
        await dbContext.SaveChangesAsync(cancellationToken);

        return AdminActionResult.Success("Форму або сорт додано.");
    }

    public async Task<AdminActionResult> UpdateVarietyAsync(
        int id,
        AdminPlantVarietyFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        var variety = await dbContext.PlantVarieties.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (variety is null)
        {
            return AdminActionResult.Missing();
        }

        var normalizedName = model.Name.Trim();
        var duplicateVariety = await dbContext.PlantVarieties.AnyAsync(item =>
            item.Id != id
            && item.PlantSpeciesId == variety.PlantSpeciesId
            && item.Type == model.Type
            && item.Name.ToLower() == normalizedName.ToLower(),
            cancellationToken);

        if (duplicateVariety)
        {
            return AdminActionResult.Failure("Така форма або сорт уже існує для цього виду.");
        }

        variety.Update(normalizedName, model.Type);
        await dbContext.SaveChangesAsync(cancellationToken);

        return AdminActionResult.Success("Форму або сорт оновлено.");
    }

    public async Task<AdminActionResult> DeleteVarietyAsync(int id, CancellationToken cancellationToken = default)
    {
        var variety = await dbContext.PlantVarieties
            .Include(item => item.Plants)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (variety is null)
        {
            return AdminActionResult.Missing();
        }

        if (variety.Plants.Count > 0)
        {
            return AdminActionResult.Failure("Форма або сорт використовується в колекціях користувачів, тому її не видалено.");
        }

        dbContext.PlantVarieties.Remove(variety);
        await dbContext.SaveChangesAsync(cancellationToken);

        return AdminActionResult.Success("Форму або сорт видалено.");
    }

    public async Task<AdminActionResult> DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        var speciesCount = await dbContext.PlantSpecies.CountAsync(cancellationToken);
        var varietyCount = await dbContext.PlantVarieties.CountAsync(cancellationToken);

        if (speciesCount == 0 && varietyCount == 0)
        {
            return AdminActionResult.Success("Каталог видів рослин уже порожній.");
        }

        await dbContext.Plants.ExecuteUpdateAsync(setters => setters
            .SetProperty(plant => plant.PlantSpeciesId, (int?)null)
            .SetProperty(plant => plant.PlantVarietyId, (int?)null),
            cancellationToken);
        await dbContext.PlantVarieties.ExecuteDeleteAsync(cancellationToken);
        await dbContext.PlantSpecies.ExecuteDeleteAsync(cancellationToken);

        return AdminActionResult.Success(
            $"Видалено каталог: {speciesCount} видів і {varietyCount} форм/сортів. Картки користувачів збережено.");
    }

    private static IQueryable<PlantSpecies> ApplySearch(IQueryable<PlantSpecies> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return query;
        }

        var pattern = $"%{search.Trim()}%";
        return query.Where(species =>
            EF.Functions.ILike(species.Name, pattern)
            || species.Varieties.Any(variety => EF.Functions.ILike(variety.Name, pattern)));
    }

    private static IQueryable<PlantSpecies> ApplyVariantFilter(
        IQueryable<PlantSpecies> query,
        string variantFilter)
    {
        return variantFilter switch
        {
            "with_variants" => query.Where(species => species.Varieties.Any()),
            "without_variants" => query.Where(species => !species.Varieties.Any()),
            _ => query
        };
    }

    private static IQueryable<PlantSpecies> ApplySorting(IQueryable<PlantSpecies> query, string sort)
    {
        return sort switch
        {
            "species_desc" => query.OrderByDescending(species => species.Name),
            "variant_count_asc" => query.OrderBy(species => species.Varieties.Count).ThenBy(species => species.Name),
            "variant_count_desc" => query.OrderByDescending(species => species.Varieties.Count).ThenBy(species => species.Name),
            _ => query.OrderBy(species => species.Name)
        };
    }
}
