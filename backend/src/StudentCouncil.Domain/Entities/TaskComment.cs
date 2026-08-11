using StudentCouncil.Domain.Common;

namespace StudentCouncil.Domain.Entities;

public class TaskComment : BaseEntity
{
    public Guid TaskItemId { get; set; }
    public TaskItem TaskItem { get; set; } = null!;

    /// <summary>FK -> Member (ApplicationUser).</summary>
    public Guid AuthorId { get; set; }

    public string Text { get; set; } = string.Empty;
}
