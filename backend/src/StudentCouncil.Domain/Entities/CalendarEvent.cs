using StudentCouncil.Domain.Common;

namespace StudentCouncil.Domain.Entities;

public class CalendarEvent : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }

    /// <summary>Room / address / link.</summary>
    public string? Location { get; set; }
    public EventType Type { get; set; }

    /// <summary>Organizing department (optional).</summary>
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    /// <summary>FK -> Member (ApplicationUser).</summary>
    public Guid OrganizerId { get; set; }
    public RecurrenceType Recurrence { get; set; }

    /// <summary>Empty = visible to everyone.</summary>
    public ICollection<EventParticipant> Participants { get; set; } = new List<EventParticipant>();

    /// <summary>Idempotency markers so each reminder fires at most once per occurrence (spec 10).</summary>
    public DateTime? Reminder24hSentAtUtc { get; set; }
    public DateTime? Reminder1hSentAtUtc { get; set; }
}
