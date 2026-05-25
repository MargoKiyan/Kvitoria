using Kvitoria.ViewModels;

namespace Kvitoria.Services.Admin;

public interface IAdminAnalyticsService
{
    Task<AdminDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default);
}
