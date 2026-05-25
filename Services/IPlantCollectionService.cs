using Kvitoria.Models;
using Kvitoria.Models.Enums;
using Kvitoria.ViewModels;

namespace Kvitoria.Services;

public interface IPlantCollectionService
{
    Task<DashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<PlantListViewModel> GetPlantsAsync(
        string? search,
        PlantStatus? status,
        int page,
        CancellationToken cancellationToken = default);

    Task<Plant?> GetPlantAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CreatePlantAsync(PlantFormViewModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdatePlantAsync(int id, PlantFormViewModel model, CancellationToken cancellationToken = default);

    Task<bool> DeletePlantAsync(int id, CancellationToken cancellationToken = default);

    Task<OperationResult> AddCareLogAsync(CareLogFormViewModel model, CancellationToken cancellationToken = default);

    Task<CareLog?> GetCareLogAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> DeleteCareLogAsync(int id, CancellationToken cancellationToken = default);
}
