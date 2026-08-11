using System.Security.Cryptography;

namespace StudentCouncil.Infrastructure.Identity;

/// <summary>Generates cryptographically strong temporary passwords that satisfy the password policy.</summary>
internal static class TemporaryPassword
{
    public static string Generate()
    {
        // URL-safe random body + a suffix that guarantees the policy (uppercase + digit).
        var body = Convert.ToBase64String(RandomNumberGenerator.GetBytes(12))
            .Replace("+", "x").Replace("/", "y").Replace("=", string.Empty);
        return $"{body}A1";
    }
}
