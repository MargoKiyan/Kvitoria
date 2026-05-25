using Kvitoria.ViewModels;

namespace Kvitoria.Services.Admin;

public interface IAdminFeedbackService
{
    Task<AdminFeedbackViewModel> GetFeedbackAsync(
        int page,
        CancellationToken cancellationToken = default);

    Task<AdminActionResult> MarkReadAsync(int id, CancellationToken cancellationToken = default);

    Task<AdminActionResult> ReplyAsync(
        int id,
        string adminUserId,
        string reply,
        CancellationToken cancellationToken = default);

    Task<AdminActionResult> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<AdminActionResult> DeleteAllAsync(CancellationToken cancellationToken = default);
}
