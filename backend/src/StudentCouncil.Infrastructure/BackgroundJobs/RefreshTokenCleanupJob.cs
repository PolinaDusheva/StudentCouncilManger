using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Options;

namespace StudentCouncil.Infrastructure.BackgroundJobs;

/// <summary>
/// Deletes expired or revoked refresh tokens (spec 6.2 / 10). The <c>job_runs</c> marker makes it
/// restart-safe — it won't re-run within the configured interval just because the host restarted (decision #5).
/// </summary>
public sealed class RefreshTokenCleanupJob : PeriodicBackgroundService
{
    private readonly TimeSpan _interval;

    public RefreshTokenCleanupJob(
        IServiceScopeFactory scopeFactory, ILogger<RefreshTokenCleanupJob> logger, IOptions<BackgroundJobOptions> options)
        : base(scopeFactory, logger)
    {
        _interval = TimeSpan.FromHours(Math.Max(1, options.Value.RefreshTokenCleanupHours));
    }

    protected override TimeSpan Interval => _interval;
    protected override string JobName => "RefreshTokenCleanup";

    public override async Task ExecuteTickAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<IAppDbContext>();
        var clock = services.GetRequiredService<IDateTime>();

        var now = clock.UtcNow;

        var lastRun = await JobRunStore.LastRunAsync(db, JobName, cancellationToken);
        if (lastRun is { } previous && now - previous < _interval)
        {
            return;
        }

        await db.RefreshTokens
            .Where(t => t.ExpiresAtUtc < now || t.RevokedAtUtc != null)
            .ExecuteDeleteAsync(cancellationToken);

        await JobRunStore.MarkRunAsync(db, JobName, now, cancellationToken);
    }
}
