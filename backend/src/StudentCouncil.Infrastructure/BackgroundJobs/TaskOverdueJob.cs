using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Notifications;
using StudentCouncil.Application.Common.Options;

namespace StudentCouncil.Infrastructure.BackgroundJobs;

/// <summary>
/// Notifies assignees and the creator about tasks whose due date has passed (spec 10). The
/// <c>OverdueNotifiedAtUtc</c> marker keeps it to one notification per due date; it is cleared when the
/// due date moves (decision #7).
/// </summary>
public sealed class TaskOverdueJob : PeriodicBackgroundService
{
    private readonly TimeSpan _interval;

    public TaskOverdueJob(
        IServiceScopeFactory scopeFactory, ILogger<TaskOverdueJob> logger, IOptions<BackgroundJobOptions> options)
        : base(scopeFactory, logger)
    {
        _interval = TimeSpan.FromMinutes(Math.Max(1, options.Value.TaskOverdueMinutes));
    }

    protected override TimeSpan Interval => _interval;
    protected override string JobName => "TaskOverdue";

    public override async Task ExecuteTickAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<IAppDbContext>();
        var clock = services.GetRequiredService<IDateTime>();
        var dispatcher = services.GetRequiredService<INotificationDispatcher>();

        var now = clock.UtcNow;

        var overdue = await db.Tasks
            .Include(t => t.Assignees)
            .Where(t => t.DueAtUtc != null
                && t.DueAtUtc < now
                && t.Status != TaskStatus.Completed
                && t.Status != TaskStatus.Cancelled
                && t.OverdueNotifiedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var task in overdue)
        {
            var recipients = task.Assignees.Select(a => a.MemberId).ToHashSet();
            if (task.CreatedById is { } creatorId)
            {
                recipients.Add(creatorId);
            }

            task.OverdueNotifiedAtUtc = now;

            var content = NotificationTemplates.TaskOverdue(task.Title);
            await dispatcher.DispatchAsync(
                NotificationType.TaskOverdue, recipients.ToList(), content.Title, content.Body,
                NotificationPayload.ForTask(task.Id), cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
