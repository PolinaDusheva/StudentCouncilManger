using Microsoft.Extensions.Logging;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Notifications;

namespace StudentCouncil.Infrastructure.Notifications.Push;

/// <summary>
/// Development/Staging/test push service: logs the recipients and message instead of sending (decision #3,
/// mirroring <c>LoggingEmailSender</c>). Nothing real is delivered when credentials are absent.
/// </summary>
public sealed class LoggingPushNotificationService : IPushNotificationService
{
    private readonly ILogger<LoggingPushNotificationService> _logger;

    public LoggingPushNotificationService(ILogger<LoggingPushNotificationService> logger) => _logger = logger;

    public Task SendAsync(
        IReadOnlyCollection<Guid> memberIds,
        NotificationType type,
        string title,
        string body,
        NotificationPayload payload,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[DEV PUSH] {Type} -> {RecipientCount} member(s) | {Title} | {Body}",
            type, memberIds.Count, title, body);
        return Task.CompletedTask;
    }
}
