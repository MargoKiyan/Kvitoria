using Kvitoria.Data;
using Kvitoria.Extensions;
using Kvitoria.Models;
using Kvitoria.Models.Auth;
using Kvitoria.Models.Enums;
using Kvitoria.Services;
using Kvitoria.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kvitoria.Controllers;

[Authorize(Roles = ApplicationRoleNames.User)]
public class PlantsController(
    KvitoriaDbContext dbContext,
    IPlantCollectionService plantCollectionService,
    ILogger<PlantsController> logger) : Controller
{
    public async Task<IActionResult> Index(
        string? search,
        PlantStatus? status,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var model = await plantCollectionService.GetPlantsAsync(search, status, page, cancellationToken);
            return View(model);
        }
        catch (Exception exception) when (DatabaseFailureDetector.IsDatabaseUnavailable(exception))
        {
            return DatabaseUnavailable(exception);
        }
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        try
        {
            var plant = await plantCollectionService.GetPlantAsync(id, cancellationToken);
            return plant is null ? NotFound() : View(plant);
        }
        catch (Exception exception) when (DatabaseFailureDetector.IsDatabaseUnavailable(exception))
        {
            return DatabaseUnavailable(exception);
        }
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var model = await BuildPlantFormAsync(new PlantFormViewModel
        {
            AcquisitionDate = DateOnly.FromDateTime(DateTime.Now),
            LastWateredDate = today
        }, cancellationToken);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PlantFormViewModel model, CancellationToken cancellationToken)
    {
        model = await BuildPlantFormAsync(model, cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var id = await plantCollectionService.CreatePlantAsync(model, cancellationToken);
            TempData["StatusMessage"] = "Рослину додано до колекції.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception exception) when (DatabaseFailureDetector.IsDatabaseUnavailable(exception))
        {
            return DatabaseUnavailable(exception);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        try
        {
            var plant = await plantCollectionService.GetPlantAsync(id, cancellationToken);

            if (plant is null)
            {
                return NotFound();
            }

            return View(await BuildPlantFormAsync(PlantToForm(plant), cancellationToken));
        }
        catch (Exception exception) when (DatabaseFailureDetector.IsDatabaseUnavailable(exception))
        {
            return DatabaseUnavailable(exception);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PlantFormViewModel model, CancellationToken cancellationToken)
    {
        if (model.Id != id)
        {
            return BadRequest();
        }

        model = await BuildPlantFormAsync(model, cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var updated = await plantCollectionService.UpdatePlantAsync(id, model, cancellationToken);

            if (!updated)
            {
                return NotFound();
            }

            TempData["StatusMessage"] = "Картку рослини оновлено.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception exception) when (DatabaseFailureDetector.IsDatabaseUnavailable(exception))
        {
            return DatabaseUnavailable(exception);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var plant = await plantCollectionService.GetPlantAsync(id, cancellationToken);
            return plant is null ? NotFound() : View(plant);
        }
        catch (Exception exception) when (DatabaseFailureDetector.IsDatabaseUnavailable(exception))
        {
            return DatabaseUnavailable(exception);
        }
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await plantCollectionService.DeletePlantAsync(id, cancellationToken);

            if (!deleted)
            {
                return NotFound();
            }

            TempData["StatusMessage"] = "Рослину видалено.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception) when (DatabaseFailureDetector.IsDatabaseUnavailable(exception))
        {
            return DatabaseUnavailable(exception);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkWatered(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await plantCollectionService.AddCareLogAsync(
                new CareLogFormViewModel
                {
                    PlantId = id,
                    ActivityType = CareActivityType.Watering,
                    PerformedOn = DateOnly.FromDateTime(DateTime.Now),
                    Notes = "Швидкий запис поливу"
                },
                cancellationToken);

            if (!result.Succeeded && result.Message.Contains("не знайдено", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            TempData["StatusMessage"] = result.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception exception) when (DatabaseFailureDetector.IsDatabaseUnavailable(exception))
        {
            return DatabaseUnavailable(exception);
        }
    }

    private IActionResult DatabaseUnavailable(Exception exception)
    {
        logger.LogWarning(exception, "PostgreSQL unavailable while handling plant request.");
        return View("~/Views/Shared/DatabaseUnavailable.cshtml", DatabaseIssueViewModel.From(exception));
    }

    private async Task<PlantFormViewModel> BuildPlantFormAsync(
        PlantFormViewModel model,
        CancellationToken cancellationToken)
    {
        var speciesOptions = await dbContext.PlantSpecies
            .AsNoTracking()
            .OrderBy(species => species.Name)
            .Select(species => new PlantSpeciesOptionViewModel
            {
                Id = species.Id,
                Name = species.Name
            })
            .ToListAsync(cancellationToken);

        var varietyOptions = await dbContext.PlantVarieties
            .AsNoTracking()
            .OrderBy(variety => variety.Name)
            .Select(variety => new PlantVarietyOptionViewModel
            {
                Id = variety.Id,
                PlantSpeciesId = variety.PlantSpeciesId,
                Name = variety.Name,
                Type = variety.Type,
                TypeName = variety.Type.GetDisplayName()
            })
            .ToListAsync(cancellationToken);

        var selectedSpeciesId = model.PlantSpeciesId != 0
            ? model.PlantSpeciesId
            : speciesOptions.FirstOrDefault()?.Id ?? 0;
        var selectedVarietyId = model.PlantVarietyId;

        if (selectedVarietyId.HasValue
            && varietyOptions.All(variety => variety.Id != selectedVarietyId.Value || variety.PlantSpeciesId != selectedSpeciesId))
        {
            selectedVarietyId = null;
        }

        var nextWateringDate = model.LastWateredDate?.AddDays(model.WateringFrequency.ToDayInterval());

        return new PlantFormViewModel
        {
            Id = model.Id,
            Name = model.Name,
            PlantSpeciesId = selectedSpeciesId,
            PlantVarietyId = selectedVarietyId,
            Species = speciesOptions.FirstOrDefault(species => species.Id == selectedSpeciesId)?.Name ?? string.Empty,
            Variety = varietyOptions.FirstOrDefault(variety => variety.Id == selectedVarietyId)?.Name,
            Status = model.Status,
            LightRequirement = model.LightRequirement,
            WateringFrequency = model.WateringFrequency,
            Location = model.Location,
            PotDiameterCm = model.PotDiameterCm,
            AcquisitionDate = model.AcquisitionDate,
            AcquisitionSource = model.AcquisitionSource,
            LastWateredDate = model.LastWateredDate,
            NextWateringDate = nextWateringDate,
            ImageUrl = model.ImageUrl,
            Photo = model.Photo,
            Notes = model.Notes,
            StatusOptions = EnumSelectListExtensions.ToSelectList<PlantStatus>(),
            LightRequirementOptions = EnumSelectListExtensions.ToSelectList<LightRequirement>(),
            WateringFrequencyOptions = EnumSelectListExtensions.ToSelectList<WateringFrequency>(),
            SpeciesOptions = speciesOptions,
            VarietyOptions = varietyOptions
        };
    }

    private static PlantFormViewModel PlantToForm(Plant plant)
    {
        return new PlantFormViewModel
        {
            Id = plant.Id,
            Name = plant.Name,
            PlantSpeciesId = plant.PlantSpeciesId ?? 0,
            PlantVarietyId = plant.PlantVarietyId,
            Species = plant.Species,
            Variety = plant.Variety,
            Status = plant.Status,
            LightRequirement = plant.LightRequirement,
            WateringFrequency = plant.WateringFrequency,
            Location = plant.Location,
            PotDiameterCm = plant.PotDiameterCm,
            AcquisitionDate = plant.AcquisitionDate,
            AcquisitionSource = plant.AcquisitionSource,
            LastWateredDate = plant.LastWateredDate,
            NextWateringDate = plant.NextWateringDate,
            ImageUrl = plant.ImageUrl,
            Notes = plant.Notes
        };
    }
}
