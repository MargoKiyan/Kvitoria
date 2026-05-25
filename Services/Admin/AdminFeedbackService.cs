using Kvitoria.Data;
using Kvitoria.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Kvitoria.Services.Admin;

public class AdminFeedbackService(KvitoriaDbContext dbContext) : IAdminFeedbackService
{
    public async Task<AdminFeedbackViewModel> GetFeedbackAsync(
        int page,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.FeedbackMessages
            .AsNoTracking()
            .Include(message => message.User)
            .OrderBy(message => message.IsRead)
            .ThenByDescending(message => message.CreatedAtUtc);
        var totalMessages = await query.CountAsync(cancellationToken);
        var pagination = PaginationViewModel.Create(
            page,
            PaginationViewModel.DefaultPageSize,
            totalMessages,
            "Feedback");
        var messages = await query
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new AdminFeedbackViewModel
        {
            Messages = messages,
            Pagination = pagination
        };
    }

    public async Task<AdminActionResult> MarkReadAsync(int id, CancellationToken cancellationToken = default)
    {
        var message = await dbContext.FeedbackMessages.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (message is null)
        {
            return AdminActionResult.Missing();
        }

        message.MarkRead();
        await dbContext.SaveChangesAsync(cancellationToken);

        return AdminActionResult.Success("Звернення позначено як прочитане.");
    }

    public async Task<AdminActionResult> ReplyAsync(
        int id,
        string adminUserId,
        string reply,
        CancellationToken cancellationToken = default)
    {
        var message = await dbContext.FeedbackMessages.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (message is null)
        {
            return AdminActionResult.Missing();
        }

        if (!string.IsNullOrWhiteSpace(message.AdminReply))
        {
            return AdminActionResult.Failure("На це звернення вже надано відповідь.");
        }

        message.Reply(adminUserId, reply);
        await dbContext.SaveChangesAsync(cancellationToken);

        return AdminActionResult.Success("Відповідь надіслано користувачу, звернення позначено як прочитане.");
    }

    public async Task<AdminActionResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var message = await dbContext.FeedbackMessages.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (message is null)
        {
            return AdminActionResult.Missing();
        }

        dbContext.FeedbackMessages.Remove(message);
        await dbContext.SaveChangesAsync(cancellationToken);

        return AdminActionResult.Success("Звернення видалено.");
    }

    public async Task<AdminActionResult> DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        var deletedCount = await dbContext.FeedbackMessages.ExecuteDeleteAsync(cancellationToken);

        return deletedCount == 0
            ? AdminActionResult.Success("Звернень для видалення немає.")
            : AdminActionResult.Success($"Видалено звернень: {deletedCount}.");
    }
}
