using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Notifications;
using StudentCouncil.Domain.Enums;
using StudentCouncil.Infrastructure.Notifications.Push;
using StudentCouncil.Infrastructure.Persistence;

namespace StudentCouncil.UnitTests.Notifications;

public class NotificationDispatcherTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"notif-dispatch-{Guid.NewGuid()}").Options);

    [Fact]
    public async Task Persists_one_in_app_notification_per_recipient()
    {
        await using var db = NewDb();
        var push = new RecordingPush();
        var dispatcher = new NotificationDispatcher(db, push, NullLogger<NotificationDispatcher>.Instance);

        var recipientA = Guid.NewGuid();
        var recipientB = Guid.NewGuid();

        await dispatcher.DispatchAsync(
            NotificationType.TaskAssigned, [recipientA, recipientB, recipientA], "Заглавие", "Тяло",
            NotificationPayload.ForTask(Guid.NewGuid()));

        // Duplicate recipient collapses to one row each.
        var rows = await db.Notifications.ToListAsync();
        rows.Should().HaveCount(2);
        rows.Select(n => n.RecipientId).Should().BeEquivalentTo([recipientA, recipientB]);
        rows.Should().OnlyContain(n => !n.IsRead && n.Type == NotificationType.TaskAssigned);
        rows.Should().OnlyContain(n => n.Payload != null && n.Payload.Contains("Task"));

        push.Calls.Should().ContainSingle();
    }

    [Fact]
    public async Task Push_failure_is_swallowed_and_in_app_rows_survive()
    {
        await using var db = NewDb();
        var dispatcher = new NotificationDispatcher(db, new ThrowingPush(), NullLogger<NotificationDispatcher>.Instance);

        var act = async () => await dispatcher.DispatchAsync(
            NotificationType.TaskComment, [Guid.NewGuid()], "Заглавие", "Тяло", NotificationPayload.ForTask(Guid.NewGuid()));

        await act.Should().NotThrowAsync();
        (await db.Notifications.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task No_recipients_writes_nothing()
    {
        await using var db = NewDb();
        var push = new RecordingPush();
        var dispatcher = new NotificationDispatcher(db, push, NullLogger<NotificationDispatcher>.Instance);

        await dispatcher.DispatchAsync(
            NotificationType.EventCreated, [], "Заглавие", "Тяло", NotificationPayload.ForEvent(Guid.NewGuid()));

        (await db.Notifications.CountAsync()).Should().Be(0);
        push.Calls.Should().BeEmpty();
    }

    private sealed class RecordingPush : IPushNotificationService
    {
        public List<int> Calls { get; } = [];

        public Task SendAsync(
            IReadOnlyCollection<Guid> memberIds, NotificationType type, string title, string body,
            NotificationPayload payload, CancellationToken cancellationToken = default)
        {
            Calls.Add(memberIds.Count);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingPush : IPushNotificationService
    {
        public Task SendAsync(
            IReadOnlyCollection<Guid> memberIds, NotificationType type, string title, string body,
            NotificationPayload payload, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("gateway down");
    }
}
