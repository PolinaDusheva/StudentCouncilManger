using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Options;

namespace StudentCouncil.Infrastructure.BackgroundJobs;

/// <summary>
/// Deletes blobs that no longer have a database row referencing them (spec 10): objects under <c>tasks/</c>
/// that are not in <c>TaskDocuments.StorageKey</c>, and objects under <c>avatars/</c> not referenced by any
/// member's photo. Runs weekly; the <c>job_runs</c> marker makes it restart-safe and idempotent (decision #5).
/// </summary>
public sealed class OrphanFileCleanupJob : PeriodicBackgroundService
{
    private readonly TimeSpan _interval;

    public OrphanFileCleanupJob(
        IServiceScopeFactory scopeFactory, ILogger<OrphanFileCleanupJob> logger, IOptions<BackgroundJobOptions> options)
        : base(scopeFactory, logger)
    {
        _interval = TimeSpan.FromHours(Math.Max(1, options.Value.OrphanFileCleanupHours));
    }

    protected override TimeSpan Interval => _interval;
    protected override string JobName => "OrphanFileCleanup";

    public override async Task ExecuteTickAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<IAppDbContext>();
        var members = services.GetRequiredService<IMemberDirectory>();
        var storage = services.GetRequiredService<IFileStorage>();
        var clock = services.GetRequiredService<IDateTime>();

        var now = clock.UtcNow;

        var lastRun = await JobRunStore.LastRunAsync(db, JobName, cancellationToken);
        if (lastRun is { } previous && now - previous < _interval)
        {
            return;
        }

        var documentKeys = await db.TaskDocuments
            .Select(d => d.StorageKey)
            .ToHashSetAsync(cancellationToken);
        await DeleteOrphansAsync(storage, "tasks/", documentKeys, cancellationToken);

        var avatarKeys = await members.Members
            .Where(m => m.PhotoUrl != null)
            .Select(m => m.PhotoUrl!)
            .ToHashSetAsync(cancellationToken);
        await DeleteOrphansAsync(storage, "avatars/", avatarKeys, cancellationToken);

        await JobRunStore.MarkRunAsync(db, JobName, now, cancellationToken);
    }

    private static async Task DeleteOrphansAsync(
        IFileStorage storage, string prefix, HashSet<string> referenced, CancellationToken cancellationToken)
    {
        await foreach (var key in storage.EnumerateKeysAsync(prefix, cancellationToken))
        {
            if (!referenced.Contains(key))
            {
                await storage.DeleteAsync(key, cancellationToken);
            }
        }
    }
}
