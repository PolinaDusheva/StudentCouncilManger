using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Notifications;
using StudentCouncil.Application.Common.Options;

namespace StudentCouncil.Infrastructure.BackgroundJobs;

/// <summary>
/// Reminds participants about upcoming single events 24 hours and 1 hour before the start (spec 10).
/// Recurring events are out of scope this phase (decision #8) — their per-occurrence reminders need an
/// occurrence ledger. The two marker columns keep each lead to a single reminder; they reset when an
/// event's <c>StartUtc</c> moves (decision #7).
/// </summary>
public sealed class EventReminderJob : PeriodicBackgroundService
{
    private readonly TimeSpan _interval;

    public EventReminderJob(
        IServiceScopeFactory scopeFactory, ILogger<EventReminderJob> logger, IOptions<BackgroundJobOptions> options)
        : base(scopeFactory, logger)
    {
        _interval = TimeSpan.FromMinutes(Math.Max(1, options.Value.EventReminderMinutes));
    }

    protected override TimeSpan Interval => _interval;
    protected override string JobName => "EventReminder";

    public override async Task ExecuteTickAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<IAppDbContext>();
        var members = services.GetRequiredService<IMemberDirectory>();
        var clock = services.GetRequiredService<IDateTime>();
        var dispatcher = services.GetRequiredService<INotificationDispatcher>();

        var now = clock.UtcNow;
        var horizon24 = now.AddHours(24);
        var horizon1 = now.AddHours(1);

        var upcoming = await db.CalendarEvents
            .Include(e => e.Participants)
            .Where(e => e.Recurrence == RecurrenceType.None
                && e.StartUtc > now
                && e.StartUtc <= horizon24
                && (e.Reminder24hSentAtUtc == null || e.Reminder1hSentAtUtc == null))
            .ToListAsync(cancellationToken);

        foreach (var calendarEvent in upcoming)
        {
            var participantIds = calendarEvent.Participants.Select(p => p.MemberId).ToList();
            var recipients = await NotificationRecipients.EventRecipientsAsync(members, participantIds, cancellationToken);

            if (calendarEvent.Reminder24hSentAtUtc == null)
            {
                calendarEvent.Reminder24hSentAtUtc = now;
                await DispatchAsync(dispatcher, calendarEvent.Id, calendarEvent.Title, recipients,
                    EventReminderLead.TwentyFourHours, cancellationToken);
            }

            if (calendarEvent.StartUtc <= horizon1 && calendarEvent.Reminder1hSentAtUtc == null)
            {
                calendarEvent.Reminder1hSentAtUtc = now;
                await DispatchAsync(dispatcher, calendarEvent.Id, calendarEvent.Title, recipients,
                    EventReminderLead.OneHour, cancellationToken);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static Task DispatchAsync(
        INotificationDispatcher dispatcher, Guid eventId, string title, IReadOnlyCollection<Guid> recipients,
        EventReminderLead lead, CancellationToken cancellationToken)
    {
        var content = NotificationTemplates.EventReminder(title, lead);
        return dispatcher.DispatchAsync(
            NotificationType.EventReminder, recipients, content.Title, content.Body,
            NotificationPayload.ForEvent(eventId), cancellationToken);
    }
}
