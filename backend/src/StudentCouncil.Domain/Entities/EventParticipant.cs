namespace StudentCouncil.Domain.Entities;

/// <summary>Join entity for the n..n relation between <see cref="CalendarEvent"/> and members.</summary>
public class EventParticipant
{
    public Guid CalendarEventId { get; set; }
    public CalendarEvent CalendarEvent { get; set; } = null!;

    /// <summary>FK -> Member (ApplicationUser).</summary>
    public Guid MemberId { get; set; }
}
