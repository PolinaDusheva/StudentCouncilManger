using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Notifications;
using StudentCouncil.Application.Common.Options;

namespace StudentCouncil.Infrastructure.BackgroundJobs;

/// <summary>
/// Notifies assignees about tasks whose due date falls within the next 24 hours (spec 10). The
/// <c>DueSoonNotifiedAtUtc</c> marker keeps it to one reminder per due date; it is cleared when a task's
/// <c>DueAtUtc</c> moves (decision #7), so a rescheduled task reminds again.
/// </summary>
public sealed class TaskDueSoonJob : PeriodicBackgroundService
{
    private readonly TimeSpan _interval;

    public TaskDueSoonJob(
        IServiceScopeFactory scopeFactory, ILogger<TaskDueSoonJob> logger, IOptions<BackgroundJobOptions> options)
        : base(scopeFactory, logger)
    {
        _interval = TimeSpan.FromMinutes(Math.Max(1, options.Value.TaskDueSoonMinutes));
    }

    protected override TimeSpan Interval => _interval;
    protected override string JobName => "TaskDueSoon";

    public override async Task ExecuteTickAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<IAppDbContext>();
        var clock = services.GetRequiredService<IDateTime>();
        var dispatcher = services.GetRequiredService<INotificationDispatcher>();

        var now = clock.UtcNow;
        var horizon = now.AddHours(24);

        var due = await db.Tasks
            .Include(t => t.Assignees)
            .Where(t => t.DueAtUtc != null
                && t.DueAtUtc > now
                && t.DueAtUtc <= horizon
                && t.Status != TaskStatus.Completed
                && t.Status != TaskStatus.Cancelled
                && t.DueSoonNotifiedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var task in due)
        {
            var assignees = task.Assignees.Select(a => a.MemberId).ToList();
            task.DueSoonNotifiedAtUtc = now;

            var content = NotificationTemplates.TaskDueSoon(task.Title);
            await dispatcher.DispatchAsync(
                NotificationType.TaskDueSoon, assignees, content.Title, content.Body,
                NotificationPayload.ForTask(task.Id), cancellationToken);
        }

        // Persist markers even for the (defensive) no-assignee case, where DispatchAsync saves nothing.
        await db.SaveChangesAsync(cancellationToken);
    }
}
