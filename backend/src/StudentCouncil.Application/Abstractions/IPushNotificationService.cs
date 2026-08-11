using StudentCouncil.Application.Common.Notifications;

namespace StudentCouncil.Application.Abstractions;

/// <summary>
/// Delivers a push message to the devices of the given members (spec 9). The implementation resolves
/// each recipient's registered <see cref="Domain.Entities.DeviceToken"/>s and routes them by platform;
/// dev/staging/test environments log instead of sending (decision #3).
/// </summary>
public interface IPushNotificationService
{
    Task SendAsync(
        IReadOnlyCollection<Guid> memberIds,
        NotificationType type,
        string title,
        string body,
        NotificationPayload payload,
        CancellationToken cancellationToken = default);
}
