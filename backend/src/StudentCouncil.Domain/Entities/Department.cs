using StudentCouncil.Domain.Common;

namespace StudentCouncil.Domain.Entities;

public class Department : BaseEntity
{
    public DepartmentCode Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Members navigate via ApplicationUser.DepartmentId (configured in Infrastructure;
    // ApplicationUser lives there so Domain stays free of an Identity reference).
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public ICollection<CalendarEvent> Events { get; set; } = new List<CalendarEvent>();
}
