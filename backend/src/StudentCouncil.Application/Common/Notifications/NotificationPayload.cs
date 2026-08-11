using System.Text.Json;

namespace StudentCouncil.Application.Common.Notifications;

/// <summary>
/// Deep-link payload carried both by the push message <c>data</c> and the in-app
/// <see cref="Domain.Entities.Notification.Payload"/> column (spec 9). <see cref="Id"/> is null for
/// notifications that target a screen rather than a single entity (e.g. a duty reminder).
/// </summary>
public sealed record NotificationPayload(string Type, Guid? Id)
{
    public static NotificationPayload ForTask(Guid id) => new("Task", id);
    public static NotificationPayload ForEvent(Guid id) => new("Event", id);

    /// <summary>Duty reminders deep-link to the caller's own duty summary, so they carry no entity id.</summary>
    public static NotificationPayload ForDuty() => new("Duty", null);

    /// <summary>Serialises to the camelCase JSON stored in the <c>jsonb</c> payload column.</summary>
    public string ToJson() => JsonSerializer.Serialize(this, JsonSerializerOptions.Web);

    /// <summary>Reads a stored payload back; returns null for missing/blank values.</summary>
    public static NotificationPayload? FromJson(string? json) =>
        string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<NotificationPayload>(json, JsonSerializerOptions.Web);
}
