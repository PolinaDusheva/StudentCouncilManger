using StudentCouncil.Domain.Common;

namespace StudentCouncil.Domain.Entities;

public class DutyRecord : BaseEntity
{
    /// <summary>The member on duty. FK -> Member (ApplicationUser).</summary>
    public Guid MemberId { get; set; }

    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }

    /// <summary>Computed as (End - Start) in minutes.</summary>
    public int DurationMinutes { get; set; }

    /// <summary>Reporting month.</summary>
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }

    /// <summary>Who recorded the duty. FK -> Member (ApplicationUser).</summary>
    public Guid RecordedById { get; set; }
    public string? Note { get; set; }
}
