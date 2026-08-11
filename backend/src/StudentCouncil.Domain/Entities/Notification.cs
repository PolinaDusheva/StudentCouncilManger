using StudentCouncil.Domain.Common;

namespace StudentCouncil.Domain.Entities;

/// <summary>Optional in-app notification center entry.</summary>
public class Notification : BaseEntity
{
    /// <summary>FK -> Member (ApplicationUser).</summary>
    public Guid RecipientId { get; set; }

    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    /// <summary>JSON deep-link payload.</summary>
    public string? Payload { get; set; }
    public bool IsRead { get; set; }
}
