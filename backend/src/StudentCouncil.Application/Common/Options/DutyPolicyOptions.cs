namespace StudentCouncil.Application.Common.Options;

/// <summary>Duty policy settings (spec 15). The monthly norm a member is expected to meet.</summary>
public sealed class DutyPolicyOptions
{
    public const string SectionName = "DutyPolicy";

    /// <summary>Minutes of duty a member must log each month to meet the norm (functional 8.3 — 2 hours).</summary>
    public int RequiredMinutesPerMonth { get; set; } = 120;
}
