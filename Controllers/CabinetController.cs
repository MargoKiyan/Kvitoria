using Kvitoria.Data;
using Kvitoria.Models.Auth;
using Kvitoria.ViewModels;
using Kvitoria.ViewModels.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kvitoria.Controllers;

[Authorize]
public class CabinetController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    KvitoriaDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);

        if (user is null)
        {
            return Challenge();
        }

        var model = await BuildCabinetViewModelAsync(user, cancellationToken);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([Bind(Prefix = "ProfileForm")] ProfileFormViewModel model, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);

        if (user is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            TempData["StatusMessage"] = "Перевірте заповнення профілю. Порожні або некоректні значення не збережено.";
            return View("Index", await BuildCabinetViewModelAsync(user, cancellationToken, profileForm: model));
        }

        user.UpdateProfile(model.FullName, model.BirthDate!.Value);
        await userManager.UpdateAsync(user);
        TempData["StatusMessage"] = "Профіль оновлено.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeLogin([Bind(Prefix = "LoginForm")] ChangeLoginViewModel model, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);

        if (user is null)
        {
            return Challenge();
        }

        var now = DateTime.UtcNow;

        if (!user.CanChangeLogin(now))
        {
            ModelState.AddModelError("LoginForm.Login", $"Логін можна змінити раз на місяць. Наступна зміна: {user.NextLoginChangeAtUtc()?.ToLocalTime():dd.MM.yyyy}.");
        }

        if (ModelState.IsValid)
        {
            var login = AccountValidationRules.NormalizeLogin(model.Login);
            var existing = await userManager.FindByNameAsync(login);

            if (existing is not null && existing.Id != user.Id)
            {
                ModelState.AddModelError("LoginForm.Login", "Такий логін вже використовується.");
            }
            else if (string.Equals(user.UserName, login, StringComparison.Ordinal))
            {
                ModelState.AddModelError("LoginForm.Login", "Новий логін збігається з поточним.");
            }
            else
            {
                var result = await userManager.SetUserNameAsync(user, login);

                if (result.Succeeded)
                {
                    user.MarkLoginChanged(now);
                    await userManager.UpdateAsync(user);
                    await signInManager.RefreshSignInAsync(user);
                    TempData["StatusMessage"] = "Логін оновлено.";

                    return RedirectToAction(nameof(Index));
                }

                AddIdentityErrors(result, "LoginForm.Login");
            }
        }

        TempData["StatusMessage"] = "Логін не оновлено. Перевірте правила введення.";
        return View("Index", await BuildCabinetViewModelAsync(user, cancellationToken, loginForm: model));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([Bind(Prefix = "PasswordForm")] ChangePasswordViewModel model, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);

        if (user is null)
        {
            return Challenge();
        }

        var now = DateTime.UtcNow;

        if (!user.CanChangePassword(now))
        {
            ModelState.AddModelError("PasswordForm.NewPassword", $"Пароль можна змінити раз на місяць. Наступна зміна: {user.NextPasswordChangeAtUtc()?.ToLocalTime():dd.MM.yyyy}.");
        }

        if (ModelState.IsValid)
        {
            var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
            {
                user.MarkPasswordChanged(now);
                await userManager.UpdateAsync(user);
                await signInManager.RefreshSignInAsync(user);
                TempData["StatusMessage"] = "Пароль оновлено.";

                return RedirectToAction(nameof(Index));
            }

            AddIdentityErrors(result, "PasswordForm.NewPassword");
        }

        TempData["StatusMessage"] = "Пароль не оновлено. Перевірте поточний пароль і правила нового пароля.";
        return View("Index", await BuildCabinetViewModelAsync(user, cancellationToken, passwordForm: model));
    }

    private async Task<CabinetViewModel> BuildCabinetViewModelAsync(
        ApplicationUser user,
        CancellationToken cancellationToken,
        ProfileFormViewModel? profileForm = null,
        ChangeLoginViewModel? loginForm = null,
        ChangePasswordViewModel? passwordForm = null)
    {
        var isAdmin = await userManager.IsInRoleAsync(user, ApplicationRoleNames.Admin);
        var tomorrow = DateOnly.FromDateTime(DateTime.Now).AddDays(1);
        var plants = isAdmin
            ? new List<Kvitoria.Models.Plant>()
            : await dbContext.Plants
                .AsNoTracking()
                .Where(plant => plant.UserId == user.Id)
                .OrderBy(plant => plant.NextWateringDate ?? DateOnly.MaxValue)
                .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var model = new CabinetViewModel
        {
            Login = user.UserName ?? string.Empty,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            BirthDate = user.BirthDate,
            RegisteredAtUtc = user.RegisteredAtUtc,
            LastLoginAtUtc = user.LastLoginAtUtc,
            NextLoginChangeAtUtc = user.NextLoginChangeAtUtc(),
            NextPasswordChangeAtUtc = user.NextPasswordChangeAtUtc(),
            CanChangeLogin = user.CanChangeLogin(now),
            CanChangePassword = user.CanChangePassword(now),
            IsAdmin = isAdmin,
            PlantCount = plants.Count,
            DueTomorrowCount = plants.Count(plant => plant.NextWateringDate == tomorrow),
            UpcomingPlants = plants.Take(6).ToList(),
            ProfileForm = profileForm ?? new ProfileFormViewModel
            {
                FullName = user.FullName,
                BirthDate = user.BirthDate
            },
            LoginForm = loginForm ?? new ChangeLoginViewModel
            {
                Login = user.UserName ?? string.Empty
            },
            PasswordForm = passwordForm ?? new ChangePasswordViewModel()
        };

        return model;
    }

    private void AddIdentityErrors(IdentityResult result, string key)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(key, error.Description);
        }
    }
}
