using StudentCouncil.Application.Common.Notifications;

namespace StudentCouncil.Application.Abstractions;

/// <summary>
/// The single notification seam the handlers and background jobs depend on (decision #1). It persists
/// one in-app <see cref="Domain.Entities.Notification"/> per recipient (the spec 7.9 center) and then
/// delegates to <see cref="IPushNotificationService"/>. Push failures are swallowed by the
/// implementation, so a notification never aborts the caller's business action.
/// </summary>
public interface INotificationDispatcher
{
    Task DispatchAsync(
        NotificationType type,
        IReadOnlyCollection<Guid> recipientIds,
        string title,
        string body,
        NotificationPayload payload,
        CancellationToken cancellationToken = default);
}
