using System.Linq.Expressions;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Application.Features.Tasks;

public static class TaskMappings
{
    /// <summary>
    /// EF-translatable projection of a <see cref="TaskItem"/> to a list row. The child counts and
    /// the department code are evaluated in SQL; <paramref name="nowUtc"/> drives the overdue flag.
    /// </summary>
    public static Expression<Func<TaskItem, TaskListItemDto>> ToListItem(DateTime nowUtc) =>
        task => new TaskListItemDto(
            task.Id,
            task.Title,
            task.Priority,
            task.Status,
            task.DueAtUtc,
            task.Scope,
            task.Department != null ? task.Department.Code : (DepartmentCode?)null,
            task.Assignees.Count,
            task.Comments.Count,
            task.Documents.Count,
            task.DueAtUtc != null
                && task.DueAtUtc < nowUtc
                && task.Status != TaskStatus.Completed
                && task.Status != TaskStatus.Cancelled);
}
