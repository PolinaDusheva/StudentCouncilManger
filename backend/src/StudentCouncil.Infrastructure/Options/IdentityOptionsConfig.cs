using System.ComponentModel.DataAnnotations;

namespace StudentCouncil.Infrastructure.Options;

public sealed class IdentityOptionsConfig
{
    public const string SectionName = "Identity";

    [Range(1, 20)]
    public int MaxFailedAttempts { get; set; } = 5;

    [Range(1, 1440)]
    public int LockoutMinutes { get; set; } = 15;
}
