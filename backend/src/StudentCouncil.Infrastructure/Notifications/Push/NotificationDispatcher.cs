using Microsoft.Extensions.Logging;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Notifications;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Infrastructure.Notifications.Push;

/// <summary>
/// The handler/job-facing notification seam (decision #1): persists one in-app
/// <see cref="Notification"/> per recipient, then delegates to <see cref="IPushNotificationService"/>.
/// A push failure is logged and swallowed — the in-app row is already saved, so the notification is never
/// lost and the caller's business action is never aborted.
/// </summary>
public sealed class NotificationDispatcher : INotificationDispatcher
{
    private readonly IAppDbContext _db;
    private readonly IPushNotificationService _push;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        IAppDbContext db, IPushNotificationService push, ILogger<NotificationDispatcher> logger)
    {
        _db = db;
        _push = push;
        _logger = logger;
    }

    public async Task DispatchAsync(
        NotificationType type,
        IReadOnlyCollection<Guid> recipientIds,
        string title,
        string body,
        NotificationPayload payload,
        CancellationToken cancellationToken = default)
    {
        var recipients = recipientIds.Distinct().ToList();
        if (recipients.Count == 0)
        {
            return;
        }

        var payloadJson = payload.ToJson();
        foreach (var recipientId in recipients)
        {
            _db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                RecipientId = recipientId,
                Type = type,
                Title = title,
                Body = body,
                Payload = payloadJson,
                IsRead = false
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            await _push.SendAsync(recipients, type, title, body, payload, cancellationToken);
        }
        catch (Exception ex)
        {
            // Push is best-effort: the in-app rows above are already persisted (decision #1).
            _logger.LogWarning(ex, "Push delivery failed for {Type}; in-app notifications were still saved.", type);
        }
    }
}
