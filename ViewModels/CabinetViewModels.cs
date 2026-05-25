using System.ComponentModel.DataAnnotations;
using Kvitoria.Models;
using Kvitoria.ViewModels.Validation;

namespace Kvitoria.ViewModels;

public class CabinetViewModel
{
    public string Login { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public DateOnly BirthDate { get; init; }

    public DateTime RegisteredAtUtc { get; init; }

    public DateTime? LastLoginAtUtc { get; init; }

    public DateTime? NextLoginChangeAtUtc { get; init; }

    public DateTime? NextPasswordChangeAtUtc { get; init; }

    public bool CanChangeLogin { get; init; }

    public bool CanChangePassword { get; init; }

    public bool IsAdmin { get; init; }

    public int PlantCount { get; init; }

    public int DueTomorrowCount { get; init; }

    public IReadOnlyList<Plant> UpcomingPlants { get; init; } = [];

    public ProfileFormViewModel ProfileForm { get; init; } = new();

    public ChangeLoginViewModel LoginForm { get; init; } = new();

    public ChangePasswordViewModel PasswordForm { get; init; } = new();
}

public class ProfileFormViewModel
{
    [Required(ErrorMessage = "Вкажіть ім'я.")]
    [StringLength(80, ErrorMessage = "Ім'я має бути до 80 символів.")]
    [Display(Name = "Ім'я")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть дату народження.")]
    [BirthDateRange]
    [DataType(DataType.Date)]
    [Display(Name = "Дата народження")]
    public DateOnly? BirthDate { get; set; }

}

public class ChangeLoginViewModel
{
    [Required(ErrorMessage = "Вкажіть новий логін.")]
    [RegularExpression(AccountValidationRules.LoginPattern, ErrorMessage = AccountValidationRules.LoginError)]
    [StringLength(40, MinimumLength = 5, ErrorMessage = "Логін має містити від 5 до 40 символів.")]
    [Display(Name = "Новий логін")]
    public string Login { get; set; } = string.Empty;
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Вкажіть поточний пароль.")]
    [DataType(DataType.Password)]
    [Display(Name = "Поточний пароль")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть новий пароль.")]
    [RegularExpression(AccountValidationRules.PasswordPattern, ErrorMessage = AccountValidationRules.PasswordError)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Пароль має містити щонайменше 8 символів.")]
    [DataType(DataType.Password)]
    [Display(Name = "Новий пароль")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Повторіть новий пароль.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Паролі не збігаються.")]
    [Display(Name = "Повтор пароля")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
