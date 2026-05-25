using Kvitoria.Models.Auth;
using Kvitoria.ViewModels;
using Kvitoria.ViewModels.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Kvitoria.Controllers;

public class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : Controller
{
    private const string RememberLoginCookieName = "Kvitoria.RememberLogin";

    [AllowAnonymous]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var login = AccountValidationRules.NormalizeLogin(model.Login);
        var email = model.Email.Trim();

        if (await userManager.FindByNameAsync(login) is not null)
        {
            ModelState.AddModelError(nameof(model.Login), "Такий логін вже використовується.");
            return View(model);
        }

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "Користувач із такою поштою вже існує.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = login,
            Email = email,
            EmailConfirmed = true
        };
        user.UpdateProfile(model.FullName, model.BirthDate!.Value);

        var result = await userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return View(model);
        }

        await userManager.AddToRoleAsync(user, ApplicationRoleNames.User);
        await signInManager.SignInAsync(user, isPersistent: false);

        TempData["StatusMessage"] = "Вітаємо у Kvitoria. Ваш кабінет створено.";
        return RedirectToAction("Index", "Cabinet");
    }

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel
        {
            Login = Request.Cookies.TryGetValue(RememberLoginCookieName, out var rememberedLogin)
                ? rememberedLogin
                : string.Empty,
            RememberMe = Request.Cookies.ContainsKey(RememberLoginCookieName)
        });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var login = AccountValidationRules.NormalizeLogin(model.Login);
        var user = await userManager.FindByNameAsync(login);

        if (user is null || user.IsDeleted)
        {
            ModelState.AddModelError(string.Empty, "Користувача не знайдено або обліковий запис деактивовано.");
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Неправильний логін або пароль.");
            return View(model);
        }

        if (model.RememberMe)
        {
            Response.Cookies.Append(
                RememberLoginCookieName,
                login,
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                    HttpOnly = true,
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax
                });
        }
        else
        {
            Response.Cookies.Delete(RememberLoginCookieName);
        }

        user.MarkLoggedIn();
        await userManager.UpdateAsync(user);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return await userManager.IsInRoleAsync(user, ApplicationRoleNames.Admin)
            ? RedirectToAction("Index", "Admin")
            : RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        TempData["StatusMessage"] = "Ви вийшли з системи.";
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
