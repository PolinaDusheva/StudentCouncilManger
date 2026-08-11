using StudentCouncil.Domain.Common;

namespace StudentCouncil.Domain.Entities;

public class TaskItem : BaseEntity, ISoftDeletable
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskPriority Priority { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.New;
    public DateTime? DueAtUtc { get; set; }
    public TaskScope Scope { get; set; }

    /// <summary>Required when <see cref="Scope"/> is <see cref="TaskScope.Departmental"/>.</summary>
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    // Creator is tracked via BaseEntity.CreatedById (FK -> Member).

    public ICollection<TaskAssignee> Assignees { get; set; } = new List<TaskAssignee>();
    public ICollection<TaskDocument> Documents { get; set; } = new List<TaskDocument>();
    public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();

    /// <summary>Idempotency markers so each reminder fires at most once per due date (spec 10).</summary>
    public DateTime? DueSoonNotifiedAtUtc { get; set; }
    public DateTime? OverdueNotifiedAtUtc { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}
