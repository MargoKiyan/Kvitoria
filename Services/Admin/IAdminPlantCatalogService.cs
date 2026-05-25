using Kvitoria.ViewModels;

namespace Kvitoria.Services.Admin;

public interface IAdminPlantCatalogService
{
    Task<AdminPlantsViewModel> GetCatalogAsync(
        string? search,
        string sort,
        string variantFilter,
        int page,
        CancellationToken cancellationToken = default);

    Task<AdminActionResult> AddSpeciesAsync(
        AdminPlantSpeciesFormViewModel model,
        CancellationToken cancellationToken = default);

    Task<AdminActionResult> UpdateSpeciesAsync(
        int id,
        AdminPlantSpeciesFormViewModel model,
        CancellationToken cancellationToken = default);

    Task<AdminActionResult> DeleteSpeciesAsync(int id, CancellationToken cancellationToken = default);

    Task<AdminActionResult> AddVarietyAsync(
        AdminPlantVarietyFormViewModel model,
        CancellationToken cancellationToken = default);

    Task<AdminActionResult> UpdateVarietyAsync(
        int id,
        AdminPlantVarietyFormViewModel model,
        CancellationToken cancellationToken = default);

    Task<AdminActionResult> DeleteVarietyAsync(int id, CancellationToken cancellationToken = default);

    Task<AdminActionResult> DeleteAllAsync(CancellationToken cancellationToken = default);
}
