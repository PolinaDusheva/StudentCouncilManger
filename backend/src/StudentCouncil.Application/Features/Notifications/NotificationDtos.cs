using StudentCouncil.Application.Common.Notifications;

namespace StudentCouncil.Application.Features.Notifications;

/// <summary>An in-app notification center entry (spec 7.9).</summary>
public sealed record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string Body,
    NotificationPayload? Payload,
    bool IsRead,
    DateTime CreatedAtUtc);
