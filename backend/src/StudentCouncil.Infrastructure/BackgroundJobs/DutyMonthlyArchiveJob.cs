using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Notifications;
using StudentCouncil.Application.Common.Options;

namespace StudentCouncil.Infrastructure.BackgroundJobs;

/// <summary>
/// "Soft" monthly close of the duty reporting period (spec 10, decision #11). Duty data is already
/// queryable by period, so there is no destructive archive: on the first tick of a new month the job logs
/// the close of the previous month and reminds anyone who finished it under norm. It ticks hourly and uses
/// the <c>job_runs</c> marker to run at most once per calendar month, restart-safe.
/// </summary>
public sealed class DutyMonthlyArchiveJob : PeriodicBackgroundService
{
    private readonly ILogger<DutyMonthlyArchiveJob> _logger;

    public DutyMonthlyArchiveJob(IServiceScopeFactory scopeFactory, ILogger<DutyMonthlyArchiveJob> logger)
        : base(scopeFactory, logger)
    {
        _logger = logger;
    }

    protected override TimeSpan Interval => TimeSpan.FromHours(1);
    protected override string JobName => "DutyMonthlyArchive";

    public override async Task ExecuteTickAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<IAppDbContext>();
        var members = services.GetRequiredService<IMemberDirectory>();
        var clock = services.GetRequiredService<IDateTime>();
        var dispatcher = services.GetRequiredService<INotificationDispatcher>();
        var dutyPolicy = services.GetRequiredService<IOptions<DutyPolicyOptions>>();

        var now = clock.UtcNow;

        // Once per calendar month: skip if we already ran during the current month.
        var lastRun = await JobRunStore.LastRunAsync(db, JobName, cancellationToken);
        if (lastRun is { } previous && previous.Year == now.Year && previous.Month == now.Month)
        {
            return;
        }

        var firstOfThisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var closedMonth = firstOfThisMonth.AddMonths(-1);

        _logger.LogInformation(
            "Closing duty reporting period {Year}-{Month:D2} (soft archive).", closedMonth.Year, closedMonth.Month);

        var required = dutyPolicy.Value.RequiredMinutesPerMonth;
        var underNorm = await NotificationRecipients.UnderDutyNormAsync(
            db, members, closedMonth.Year, closedMonth.Month, required, cancellationToken);

        if (underNorm.Count > 0)
        {
            var content = NotificationTemplates.DutyReminder(closedMonth.Year, closedMonth.Month);
            await dispatcher.DispatchAsync(
                NotificationType.DutyReminder, underNorm, content.Title, content.Body,
                NotificationPayload.ForDuty(), cancellationToken);
        }

        await JobRunStore.MarkRunAsync(db, JobName, now, cancellationToken);
    }
}
