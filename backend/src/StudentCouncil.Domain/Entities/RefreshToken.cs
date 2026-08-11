namespace StudentCouncil.Domain.Entities;

/// <summary>
/// Opaque refresh token. Only the SHA-256 hash of the raw token is stored.
/// Rotated on every refresh; reuse of a rotated token invalidates the chain.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }

    /// <summary>FK -> Member (ApplicationUser).</summary>
    public Guid MemberId { get; set; }

    /// <summary>SHA-256 hash of the raw token.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>Hash of the token that replaced this one on rotation.</summary>
    public string? ReplacedByTokenHash { get; set; }

    public bool IsActive => RevokedAtUtc is null && DateTime.UtcNow < ExpiresAtUtc;
}
