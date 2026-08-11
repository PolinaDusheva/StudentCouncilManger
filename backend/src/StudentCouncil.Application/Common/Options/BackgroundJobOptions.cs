namespace StudentCouncil.Application.Common.Options;

/// <summary>
/// Background job scheduling (spec 10, section "BackgroundJobs"). Read lazily via <c>IOptions</c> so the
/// integration-test host can disable the timers (<see cref="Enabled"/> = false) after registration.
/// </summary>
public sealed class BackgroundJobOptions
{
    public const string SectionName = "BackgroundJobs";

    /// <summary>When false, no timers are hosted — jobs can still be invoked directly in tests.</summary>
    public bool Enabled { get; set; } = true;

    public int TaskDueSoonMinutes { get; set; } = 15;
    public int TaskOverdueMinutes { get; set; } = 60;
    public int EventReminderMinutes { get; set; } = 15;
    public int RefreshTokenCleanupHours { get; set; } = 24;
    public int OrphanFileCleanupHours { get; set; } = 168;
}
