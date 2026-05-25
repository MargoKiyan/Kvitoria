using Kvitoria.Models.Auth;
using Kvitoria.Services.Admin;
using Kvitoria.Services.Reports;
using Kvitoria.ViewModels;
using Kvitoria.ViewModels.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Kvitoria.Controllers;

[Authorize(Roles = ApplicationRoleNames.Admin)]
public class AdminController(
    UserManager<ApplicationUser> userManager,
    IAdminAnalyticsService analyticsService,
    IAdminUserService adminUserService,
    IAdminPlantCatalogService plantCatalogService,
    IAdminFeedbackService feedbackService,
    IReportService reportService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await analyticsService.GetDashboardAsync(cancellationToken);
        return View(model);
    }

    public async Task<IActionResult> Users(
        string? search,
        string status = "all",
        string role = "all",
        string sort = "login_asc",
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var model = await adminUserService.GetUsersAsync(search, status, role, sort, page, cancellationToken);
        return View("~/Views/Admin/Entities/Users.cshtml", model);
    }

    public async Task<IActionResult> EditUser(string id)
    {
        var user = await userManager.FindByIdAsync(id);

        return user is null
            ? NotFound()
            : View("~/Views/Admin/Users/Edit.cshtml", UserToEditModel(user));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(string id, AdminUserEditViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var user = await userManager.FindByIdAsync(id);

        if (user is null)
        {
            return NotFound();
        }

        if (await userManager.IsInRoleAsync(user, ApplicationRoleNames.Admin) && model.IsDeleted)
        {
            ModelState.AddModelError(nameof(model.IsDeleted), "Адміністратора не можна видалити з цієї панелі.");
        }

        if (ModelState.IsValid)
        {
            var login = AccountValidationRules.NormalizeLogin(model.Login);
            var existing = await userManager.FindByNameAsync(login);

            if (existing is not null && existing.Id != user.Id)
            {
                ModelState.AddModelError(nameof(model.Login), "Такий логін вже використовується.");
            }
        }

        if (!ModelState.IsValid)
        {
            model.Email = user.Email ?? model.Email;
            return View("~/Views/Admin/Users/Edit.cshtml", model);
        }

        var normalizedLogin = AccountValidationRules.NormalizeLogin(model.Login);

        if (!string.Equals(user.UserName, normalizedLogin, StringComparison.Ordinal))
        {
            var loginResult = await userManager.SetUserNameAsync(user, normalizedLogin);

            if (!loginResult.Succeeded)
            {
                AddIdentityErrors(loginResult);
                model.Email = user.Email ?? model.Email;
                return View("~/Views/Admin/Users/Edit.cshtml", model);
            }
        }

        user.UpdateProfile(model.FullName, model.BirthDate!.Value);

        if (model.IsDeleted && !user.IsDeleted)
        {
            user.SoftDelete();
        }
        else if (!model.IsDeleted && user.IsDeleted)
        {
            user.Restore();
        }

        var result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            model.Email = user.Email ?? model.Email;
            return View("~/Views/Admin/Users/Edit.cshtml", model);
        }

        TempData["StatusMessage"] = "Акаунт користувача оновлено.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await userManager.FindByIdAsync(id);

        if (user is null)
        {
            return NotFound();
        }

        if (await userManager.IsInRoleAsync(user, ApplicationRoleNames.Admin))
        {
            TempData["StatusMessage"] = "Адміністратора не можна деактивувати з цієї панелі.";
            return RedirectToAction(nameof(Users));
        }

        user.SoftDelete();
        await userManager.UpdateAsync(user);
        TempData["StatusMessage"] = "Користувача деактивовано.";

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreUser(string id)
    {
        var user = await userManager.FindByIdAsync(id);

        if (user is null)
        {
            return NotFound();
        }

        user.Restore();
        await userManager.UpdateAsync(user);
        TempData["StatusMessage"] = "Користувача відновлено.";

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAllUsers(CancellationToken cancellationToken)
    {
        var result = await adminUserService.DeleteAllRegularUsersAsync(cancellationToken);
        return RedirectWithStatus(result, nameof(Users));
    }

    public async Task<IActionResult> Plants(
        string? search,
        string sort = "species_asc",
        string variantFilter = "all",
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var model = await plantCatalogService.GetCatalogAsync(search, sort, variantFilter, page, cancellationToken);
        return View("~/Views/Admin/Entities/Plants.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPlantSpecies(AdminPlantSpeciesFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["StatusMessage"] = "Вид не додано. Перевірте назву.";
            return RedirectToAction(nameof(Plants));
        }

        var result = await plantCatalogService.AddSpeciesAsync(model, cancellationToken);
        return RedirectWithStatus(result, nameof(Plants));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePlantSpecies(int id, AdminPlantSpeciesFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["StatusMessage"] = "Вид не оновлено. Перевірте назву.";
            return RedirectToAction(nameof(Plants));
        }

        var result = await plantCatalogService.UpdateSpeciesAsync(id, model, cancellationToken);
        return RedirectWithStatus(result, nameof(Plants));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePlantSpecies(int id, CancellationToken cancellationToken)
    {
        var result = await plantCatalogService.DeleteSpeciesAsync(id, cancellationToken);
        return RedirectWithStatus(result, nameof(Plants));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPlantVariety(AdminPlantVarietyFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["StatusMessage"] = "Форму або сорт не додано. Перевірте дані.";
            return RedirectToAction(nameof(Plants));
        }

        var result = await plantCatalogService.AddVarietyAsync(model, cancellationToken);
        return RedirectWithStatus(result, nameof(Plants));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePlantVariety(int id, AdminPlantVarietyFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["StatusMessage"] = "Форму або сорт не оновлено. Перевірте дані.";
            return RedirectToAction(nameof(Plants));
        }

        var result = await plantCatalogService.UpdateVarietyAsync(id, model, cancellationToken);
        return RedirectWithStatus(result, nameof(Plants));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePlantVariety(int id, CancellationToken cancellationToken)
    {
        var result = await plantCatalogService.DeleteVarietyAsync(id, cancellationToken);
        return RedirectWithStatus(result, nameof(Plants));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAllPlantCatalog(CancellationToken cancellationToken)
    {
        var result = await plantCatalogService.DeleteAllAsync(cancellationToken);
        return RedirectWithStatus(result, nameof(Plants));
    }

    public async Task<IActionResult> Feedback(int page = 1, CancellationToken cancellationToken = default)
    {
        var model = await feedbackService.GetFeedbackAsync(page, cancellationToken);
        return View("~/Views/Admin/Feedback/Index.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkFeedbackRead(int id, int page = 1, CancellationToken cancellationToken = default)
    {
        var result = await feedbackService.MarkReadAsync(id, cancellationToken);
        return RedirectWithStatus(result, nameof(Feedback), new { page });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplyFeedback(
        int id,
        AdminFeedbackReplyViewModel model,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var admin = await userManager.GetUserAsync(User);

        if (admin is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            TempData["StatusMessage"] = "Відповідь не надіслано: текст відповіді обов'язковий.";
            return RedirectToAction(nameof(Feedback), new { page });
        }

        var result = await feedbackService.ReplyAsync(id, admin.Id, model.Reply, cancellationToken);
        return RedirectWithStatus(result, nameof(Feedback), new { page });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFeedback(int id, int page = 1, CancellationToken cancellationToken = default)
    {
        var result = await feedbackService.DeleteAsync(id, cancellationToken);
        return RedirectWithStatus(result, nameof(Feedback), new { page });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAllFeedback(CancellationToken cancellationToken)
    {
        var result = await feedbackService.DeleteAllAsync(cancellationToken);
        return RedirectWithStatus(result, nameof(Feedback));
    }

    public IActionResult Reports()
    {
        return View("~/Views/Admin/Reports/Index.cshtml");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateTextReport(CancellationToken cancellationToken)
    {
        var file = await reportService.GenerateTextReportAsync(cancellationToken);
        TempData["StatusMessage"] = $"TXT звіт створено: {file.Name}";

        return PhysicalFile(file.FullName, "text/plain", file.Name);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateWordReport(CancellationToken cancellationToken)
    {
        var file = await reportService.GenerateWordReportAsync(cancellationToken);
        TempData["StatusMessage"] = $"Word звіт створено: {file.Name}";

        return PhysicalFile(
            file.FullName,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            file.Name);
    }

    private static AdminUserEditViewModel UserToEditModel(ApplicationUser user)
    {
        return new AdminUserEditViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Login = user.UserName ?? string.Empty,
            FullName = user.FullName,
            BirthDate = user.BirthDate,
            IsDeleted = user.IsDeleted
        };
    }

    private IActionResult RedirectWithStatus(
        AdminActionResult result,
        string actionName,
        object? routeValues = null)
    {
        if (result.NotFound)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = result.Message;
        return RedirectToAction(actionName, routeValues);
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
