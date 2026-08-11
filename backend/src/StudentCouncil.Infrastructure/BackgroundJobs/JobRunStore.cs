using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Infrastructure.BackgroundJobs;

/// <summary>
/// Reads/writes the <c>job_runs</c> markers that give cron-style jobs restart-safe idempotency
/// (decision #5): a daily/monthly task checks its last run before doing work and records the new one after.
/// </summary>
internal static class JobRunStore
{
    public static async Task<DateTime?> LastRunAsync(IAppDbContext db, string jobName, CancellationToken cancellationToken)
    {
        var run = await db.JobRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.JobName == jobName, cancellationToken);
        return run?.LastRunUtc;
    }

    public static async Task MarkRunAsync(IAppDbContext db, string jobName, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var run = await db.JobRuns.FirstOrDefaultAsync(j => j.JobName == jobName, cancellationToken);
        if (run is null)
        {
            db.JobRuns.Add(new JobRun { JobName = jobName, LastRunUtc = nowUtc });
        }
        else
        {
            run.LastRunUtc = nowUtc;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
