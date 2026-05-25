using Microsoft.AspNetCore.Identity;

namespace Kvitoria.Models.Auth;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; private set; } = string.Empty;

    public DateOnly BirthDate { get; private set; } = new(1990, 1, 1);

    public DateTime RegisteredAtUtc { get; private set; } = DateTime.UtcNow;

    public DateTime? LastLoginAtUtc { get; private set; }

    public DateTime? LastLoginChangedAtUtc { get; private set; }

    public DateTime? LastPasswordChangedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAtUtc { get; private set; }

    public ICollection<Plant> Plants { get; private set; } = new List<Plant>();

    public void UpdateProfile(string fullName, DateOnly birthDate)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Ім'я користувача обов'язкове.", nameof(fullName));
        }

        ValidateBirthDate(birthDate);

        FullName = fullName.Trim();
        BirthDate = birthDate;
    }

    public void MarkLoginChanged(DateTime changedAtUtc)
    {
        LastLoginChangedAtUtc = changedAtUtc;
    }

    public void MarkPasswordChanged(DateTime changedAtUtc)
    {
        LastPasswordChangedAtUtc = changedAtUtc;
    }

    public bool CanChangeLogin(DateTime nowUtc)
    {
        return LastLoginChangedAtUtc is null || LastLoginChangedAtUtc.Value.AddMonths(1) <= nowUtc;
    }

    public bool CanChangePassword(DateTime nowUtc)
    {
        return LastPasswordChangedAtUtc is null || LastPasswordChangedAtUtc.Value.AddMonths(1) <= nowUtc;
    }

    public DateTime? NextLoginChangeAtUtc()
    {
        return LastLoginChangedAtUtc?.AddMonths(1);
    }

    public DateTime? NextPasswordChangeAtUtc()
    {
        return LastPasswordChangedAtUtc?.AddMonths(1);
    }

    public void MarkLoggedIn()
    {
        LastLoginAtUtc = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAtUtc = DateTime.UtcNow;
        LockoutEnabled = true;
        LockoutEnd = DateTimeOffset.MaxValue;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAtUtc = null;
        LockoutEnd = null;
    }

    private static void ValidateBirthDate(DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        if (birthDate < today.AddYears(-120) || birthDate > today.AddYears(-14))
        {
            throw new ArgumentOutOfRangeException(nameof(birthDate), "Вік користувача має бути від 14 до 120 років.");
        }
    }
}
