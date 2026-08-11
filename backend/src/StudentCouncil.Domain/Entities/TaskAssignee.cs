namespace StudentCouncil.Domain.Entities;

/// <summary>Join entity for the n..n relation between <see cref="TaskItem"/> and members.</summary>
public class TaskAssignee
{
    public Guid TaskItemId { get; set; }
    public TaskItem TaskItem { get; set; } = null!;

    /// <summary>FK -> Member (ApplicationUser).</summary>
    public Guid MemberId { get; set; }
}
