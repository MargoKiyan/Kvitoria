using System.Security.Cryptography;
using System.Text;
using Kvitoria.Models.Auth;
using Microsoft.AspNetCore.Identity;

namespace Kvitoria.Services.Auth;

public sealed class Sha256PasswordHasher : IPasswordHasher<ApplicationUser>
{
    private const string Prefix = "SHA256";
    private const int SaltSize = 16;
    private readonly PasswordHasher<ApplicationUser> fallbackHasher = new();

    public string HashPassword(ApplicationUser user, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = ComputeHash(salt, password);

        return $"{Prefix}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public PasswordVerificationResult VerifyHashedPassword(
        ApplicationUser user,
        string hashedPassword,
        string providedPassword)
    {
        if (hashedPassword.StartsWith($"{Prefix}$", StringComparison.Ordinal))
        {
            return VerifySha256(hashedPassword, providedPassword);
        }

        var fallbackResult = fallbackHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);

        return fallbackResult == PasswordVerificationResult.Success
            ? PasswordVerificationResult.SuccessRehashNeeded
            : fallbackResult;
    }

    private static PasswordVerificationResult VerifySha256(string hashedPassword, string providedPassword)
    {
        var parts = hashedPassword.Split('$');

        if (parts.Length != 3)
        {
            return PasswordVerificationResult.Failed;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[1]);
            var expectedHash = Convert.FromBase64String(parts[2]);
            var actualHash = ComputeHash(salt, providedPassword);

            return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash)
                ? PasswordVerificationResult.Success
                : PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return PasswordVerificationResult.Failed;
        }
    }

    private static byte[] ComputeHash(byte[] salt, string password)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var input = new byte[salt.Length + passwordBytes.Length];

        Buffer.BlockCopy(salt, 0, input, 0, salt.Length);
        Buffer.BlockCopy(passwordBytes, 0, input, salt.Length, passwordBytes.Length);

        return SHA256.HashData(input);
    }
}
