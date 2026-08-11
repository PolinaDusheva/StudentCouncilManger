using StudentCouncil.Application.Common.Notifications;

namespace StudentCouncil.Infrastructure.Notifications.Push;

/// <summary>A single push message ready for a platform provider (decision #2).</summary>
public sealed record PushMessage(NotificationType Type, string Title, string Body, NotificationPayload Payload);

/// <summary>
/// Per-platform delivery (FCM / APNs). The provider returns the subset of tokens the gateway reported as
/// invalid/expired, so <see cref="PushNotificationService"/> can prune them (spec 9 token cleanup).
/// </summary>
public interface IPushProvider
{
    DevicePlatform Platform { get; }

    Task<IReadOnlyCollection<string>> SendAsync(
        IReadOnlyCollection<string> tokens, PushMessage message, CancellationToken cancellationToken);
}
