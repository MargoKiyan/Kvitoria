using Kvitoria.Data;
using Kvitoria.Extensions;
using Kvitoria.Models.Auth;
using Kvitoria.Models.Enums;
using Kvitoria.Services;
using Kvitoria.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kvitoria.Controllers;

[Authorize(Roles = ApplicationRoleNames.User)]
public class CareLogsController(
    IPlantCollectionService plantCollectionService,
    ILogger<CareLogsController> logger) : Controller
{
    public async Task<IActionResult> Create(int plantId, CancellationToken cancellationToken)
    {
        try
        {
            var plant = await plantCollectionService.GetPlantAsync(plantId, cancellationToken);

            if (plant is null)
            {
                return NotFound();
            }

            var model = BuildCareLogForm(new CareLogFormViewModel
            {
                PlantId = plant.Id,
                PlantName = plant.Name,
                PerformedOn = DateOnly.FromDateTime(DateTime.Now)
            });

            return View(model);
        }
        catch (Exception exception) when (DatabaseFailureDetector.IsDatabaseUnavailable(exception))
        {
            return DatabaseUnavailable(exception);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CareLogFormViewModel model, CancellationToken cancellationToken)
    {
        model = BuildCareLogForm(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var result = await plantCollectionService.AddCareLogAsync(model, cancellationToken);

            if (!result.Succeeded && result.Message.Contains("не знайдено", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            TempData["StatusMessage"] = result.Message;
            return RedirectToAction("Details", "Plants", new { id = model.PlantId });
        }
        catch (Exception exception) when (DatabaseFailureDetector.IsDatabaseUnavailable(exception))
        {
            return DatabaseUnavailable(exception);
        }
    }

    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var careLog = await plantCollectionService.GetCareLogAsync(id, cancellationToken);
            return careLog is null ? NotFound() : View(careLog);
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
            var careLog = await plantCollectionService.GetCareLogAsync(id, cancellationToken);

            if (careLog is null)
            {
                return NotFound();
            }

            var plantId = careLog.PlantId;
            var deleted = await plantCollectionService.DeleteCareLogAsync(id, cancellationToken);

            if (!deleted)
            {
                return NotFound();
            }

            TempData["StatusMessage"] = "Запис догляду видалено.";
            return RedirectToAction("Details", "Plants", new { id = plantId });
        }
        catch (Exception exception) when (DatabaseFailureDetector.IsDatabaseUnavailable(exception))
        {
            return DatabaseUnavailable(exception);
        }
    }

    private IActionResult DatabaseUnavailable(Exception exception)
    {
        logger.LogWarning(exception, "PostgreSQL unavailable while handling care log request.");
        return View("~/Views/Shared/DatabaseUnavailable.cshtml", DatabaseIssueViewModel.From(exception));
    }

    private static CareLogFormViewModel BuildCareLogForm(CareLogFormViewModel model)
    {
        return new CareLogFormViewModel
        {
            PlantId = model.PlantId,
            PlantName = model.PlantName,
            ActivityType = model.ActivityType,
            PerformedOn = model.PerformedOn,
            Notes = model.Notes,
            ActivityTypeOptions = EnumSelectListExtensions.ToSelectList<CareActivityType>()
        };
    }
}
