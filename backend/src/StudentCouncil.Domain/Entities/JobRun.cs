namespace StudentCouncil.Domain.Entities;

/// <summary>
/// Last-run marker for the cron-style background jobs (cleanup/archive), keyed by job name. Gives those
/// jobs restart-safe idempotency: a daily/monthly task won't re-run too soon after a host restart (spec 10).
/// </summary>
public class JobRun
{
    /// <summary>Stable job identifier and primary key.</summary>
    public string JobName { get; set; } = string.Empty;

    public DateTime LastRunUtc { get; set; }
}
