using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StudentCouncil.Application.Common.Notifications;
using StudentCouncil.Domain.Entities;
using StudentCouncil.Domain.Enums;
using StudentCouncil.Infrastructure.Notifications.Push;
using StudentCouncil.Infrastructure.Persistence;

namespace StudentCouncil.UnitTests.Notifications;

public class PushNotificationServiceTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"push-service-{Guid.NewGuid()}").Options);

    private static DeviceToken Device(Guid memberId, string token, DevicePlatform platform) => new()
    {
        Id = Guid.NewGuid(),
        MemberId = memberId,
        Token = token,
        Platform = platform,
        CreatedAtUtc = DateTime.UtcNow,
        LastSeenUtc = DateTime.UtcNow
    };

    [Fact]
    public async Task Routes_tokens_to_the_provider_for_their_platform()
    {
        var member = Guid.NewGuid();
        await using var db = NewDb();
        db.DeviceTokens.AddRange(
            Device(member, "android-1", DevicePlatform.Android),
            Device(member, "android-2", DevicePlatform.Android),
            Device(member, "ios-1", DevicePlatform.iOS));
        await db.SaveChangesAsync();

        var fcm = new FakeProvider(DevicePlatform.Android);
        var apns = new FakeProvider(DevicePlatform.iOS);
        var service = new PushNotificationService(db, [fcm, apns], NullLogger<PushNotificationService>.Instance);

        await service.SendAsync(
            [member], NotificationType.TaskAssigned, "T", "B", NotificationPayload.ForTask(Guid.NewGuid()));

        fcm.Received.Should().BeEquivalentTo(["android-1", "android-2"]);
        apns.Received.Should().BeEquivalentTo(["ios-1"]);
    }

    [Fact]
    public async Task Prunes_tokens_the_provider_reports_as_invalid()
    {
        var member = Guid.NewGuid();
        await using var db = NewDb();
        db.DeviceTokens.AddRange(
            Device(member, "good", DevicePlatform.Android),
            Device(member, "stale", DevicePlatform.Android));
        await db.SaveChangesAsync();

        var fcm = new FakeProvider(DevicePlatform.Android) { Invalid = ["stale"] };
        var service = new PushNotificationService(db, [fcm], NullLogger<PushNotificationService>.Instance);

        await service.SendAsync(
            [member], NotificationType.TaskDueSoon, "T", "B", NotificationPayload.ForTask(Guid.NewGuid()));

        var remaining = await db.DeviceTokens.Select(d => d.Token).ToListAsync();
        remaining.Should().BeEquivalentTo(["good"]);
    }

    [Fact]
    public async Task No_tokens_means_no_provider_calls()
    {
        await using var db = NewDb();
        var fcm = new FakeProvider(DevicePlatform.Android);
        var service = new PushNotificationService(db, [fcm], NullLogger<PushNotificationService>.Instance);

        await service.SendAsync(
            [Guid.NewGuid()], NotificationType.EventReminder, "T", "B", NotificationPayload.ForEvent(Guid.NewGuid()));

        fcm.Received.Should().BeEmpty();
    }

    private sealed class FakeProvider : IPushProvider
    {
        public FakeProvider(DevicePlatform platform) => Platform = platform;

        public DevicePlatform Platform { get; }
        public List<string> Received { get; } = [];
        public IReadOnlyCollection<string> Invalid { get; init; } = [];

        public Task<IReadOnlyCollection<string>> SendAsync(
            IReadOnlyCollection<string> tokens, PushMessage message, CancellationToken cancellationToken)
        {
            Received.AddRange(tokens);
            return Task.FromResult(Invalid);
        }
    }
}
