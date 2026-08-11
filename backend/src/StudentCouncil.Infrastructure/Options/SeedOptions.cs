using System.ComponentModel.DataAnnotations;

namespace StudentCouncil.Infrastructure.Options;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    [Required]
    [EmailAddress]
    public string AdminEmail { get; set; } = string.Empty;

    /// <summary>Optional fixed dev password; if empty a random one is generated and logged (Development only).</summary>
    public string? AdminPassword { get; set; }

    public string AdminFullName { get; set; } = "Organisation President";

    /// <summary>Role for the seeded bootstrap admin. Defaults to OrgPresident (spec §16); override via
    /// config (e.g. OrgVicePresident) when the bootstrapping user holds a different organisation role.</summary>
    public string AdminRole { get; set; } = "OrgPresident";
}
