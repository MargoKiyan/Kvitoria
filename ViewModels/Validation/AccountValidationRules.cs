using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Kvitoria.ViewModels.Validation;

public static partial class AccountValidationRules
{
    public const string LoginPattern = "^[a-z][a-z0-9._]{4,}$";

    public const string EmailPattern = @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$";

    public const string PasswordPattern = "^[A-Z][A-Za-z0-9!@#$%^&*._-]{7,}$";

    public const string LoginError =
        "Логін має починатися з малої англійської літери, містити щонайменше 5 символів: a-z, 0-9, крапка або нижнє підкреслення, без пробілів.";

    public const string PasswordError =
        "Пароль має містити щонайменше 8 символів, починатися з великої англійської літери, використовувати лише англійські літери, цифри або !@#$%^&*._-, без пробілів.";

    public const string EmailError =
        "Email має бути у форматі name@example.com.";

    public static string NormalizeLogin(string login)
    {
        return login.Trim().ToLowerInvariant();
    }

    public static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}

public sealed class BirthDateRangeAttribute : ValidationAttribute
{
    public BirthDateRangeAttribute()
    {
        ErrorMessage = "Дата народження має відповідати віку від 14 до 120 років.";
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return false;
        }

        if (value is not DateOnly birthDate)
        {
            return false;
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var minimumBirthDate = today.AddYears(-120);
        var maximumBirthDate = today.AddYears(-14);

        return birthDate >= minimumBirthDate && birthDate <= maximumBirthDate;
    }
}
