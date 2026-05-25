using Kvitoria.Data;
using Kvitoria.Models.Auth;
using Kvitoria.Models.Feedback;
using Kvitoria.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kvitoria.Controllers;

[Authorize]
public class FeedbackController(
    UserManager<ApplicationUser> userManager,
    KvitoriaDbContext dbContext) : Controller
{
    public async Task<IActionResult> Create(int page = 1, CancellationToken cancellationToken = default)
    {
        var user = await userManager.GetUserAsync(User);

        if (user is null)
        {
            return Challenge();
        }

        return View(await BuildPageModelAsync(user.Id, new FeedbackFormViewModel(), page, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "Form")] FeedbackFormViewModel model, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);

        if (user is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildPageModelAsync(user.Id, model, 1, cancellationToken));
        }

        dbContext.FeedbackMessages.Add(new FeedbackMessage(user.Id, model.Subject, model.Body));
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = "Повідомлення надіслано адміністратору.";

        return RedirectToAction(nameof(Create));
    }

    private async Task<FeedbackPageViewModel> BuildPageModelAsync(
        string userId,
        FeedbackFormViewModel form,
        int page,
        CancellationToken cancellationToken)
    {
        var query = dbContext.FeedbackMessages
            .AsNoTracking()
            .Where(message => message.UserId == userId)
            .OrderByDescending(message => message.CreatedAtUtc);
        var totalMessages = await query.CountAsync(cancellationToken);
        var pagination = PaginationViewModel.Create(
            page,
            PaginationViewModel.DefaultPageSize,
            totalMessages,
            "Create");
        var messages = await query
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new FeedbackPageViewModel
        {
            Form = form,
            Messages = messages,
            Pagination = pagination
        };
    }
}
