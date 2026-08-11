using System.ComponentModel.DataAnnotations;

namespace StudentCouncil.Infrastructure.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int AccessTokenMinutes { get; set; } = 120;

    [Range(1, 365)]
    public int RefreshTokenDays { get; set; } = 30;

    /// <summary>HS256 symmetric key — at least 256 bits (32 bytes / chars).</summary>
    [Required]
    [MinLength(32, ErrorMessage = "Jwt:SigningKey must be at least 32 characters (256 bits) for HS256.")]
    public string SigningKey { get; set; } = string.Empty;
}
