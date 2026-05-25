using System.ComponentModel.DataAnnotations;
using Kvitoria.ViewModels.Validation;

namespace Kvitoria.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Вкажіть логін.")]
    [RegularExpression(AccountValidationRules.LoginPattern, ErrorMessage = AccountValidationRules.LoginError)]
    [StringLength(40, MinimumLength = 5, ErrorMessage = "Логін має містити від 5 до 40 символів.")]
    [Display(Name = "Логін")]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть ім'я.")]
    [StringLength(80, ErrorMessage = "Ім'я має бути до 80 символів.")]
    [Display(Name = "Ім'я")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть дату народження.")]
    [BirthDateRange]
    [DataType(DataType.Date)]
    [Display(Name = "Дата народження")]
    public DateOnly? BirthDate { get; set; }

    [Required(ErrorMessage = "Вкажіть email.")]
    [EmailAddress(ErrorMessage = "Вкажіть коректний email.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть пароль.")]
    [RegularExpression(AccountValidationRules.PasswordPattern, ErrorMessage = AccountValidationRules.PasswordError)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Пароль має містити щонайменше 8 символів.")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Повторіть пароль.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Паролі не збігаються.")]
    [Display(Name = "Повтор пароля")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class LoginViewModel
{
    [Required(ErrorMessage = "Вкажіть логін.")]
    [RegularExpression(AccountValidationRules.LoginPattern, ErrorMessage = AccountValidationRules.LoginError)]
    [Display(Name = "Логін")]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть пароль.")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Запам'ятати мене")]
    public bool RememberMe { get; set; }
}
