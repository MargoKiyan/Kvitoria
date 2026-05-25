using Kvitoria.ViewModels;

namespace Kvitoria.Services.Admin;

public interface IAdminUserService
{
    Task<AdminUsersViewModel> GetUsersAsync(
        string? search,
        string status,
        string role,
        string sort,
        int page,
        CancellationToken cancellationToken = default);

    Task<AdminActionResult> DeleteAllRegularUsersAsync(CancellationToken cancellationToken = default);
}
