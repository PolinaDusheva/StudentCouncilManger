using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Application.Common.Security;

/// <summary>
/// Central authority for task visibility and write rules (spec 6.3, functional 6.4/6.5).
/// The list version shapes an <see cref="IQueryable{T}"/> so filtering happens in SQL; the
/// single-item versions apply the same rule to a loaded <see cref="TaskItem"/>. Invisible
/// tasks surface as <see cref="NotFoundException"/> (404), never 403, so existence is not leaked.
/// </summary>
public static class TaskAccess
{
    public static bool IsOrg(SystemRole role) =>
        role is SystemRole.OrgPresident or SystemRole.OrgVicePresident or SystemRole.OrgSecretary;

    public static bool IsDeptLeadership(SystemRole role) =>
        role is SystemRole.DeptPresident or SystemRole.DeptVicePresident or SystemRole.DeptSecretary;

    /// <summary>Restricts a task query to those the caller may see.</summary>
    public static IQueryable<TaskItem> Visible(IQueryable<TaskItem> source, ICurrentUser user)
    {
        var userId = user.Id ?? throw new UnauthorizedException();
        var role = user.Role ?? throw new UnauthorizedException();

        if (IsOrg(role))
        {
            return source;
        }

        if (IsDeptLeadership(role))
        {
            var deptId = user.DepartmentId;
            return source.Where(t =>
                t.CreatedById == userId
                || t.Assignees.Any(a => a.MemberId == userId)
                || (t.Scope == TaskScope.Departmental && t.DepartmentId == deptId));
        }

        // Member: only tasks they created or are assigned to.
        return source.Where(t => t.CreatedById == userId || t.Assignees.Any(a => a.MemberId == userId));
    }

    /// <summary>True when the caller is one of the task's assignees.</summary>
    public static bool IsAssignee(TaskItem task, Guid userId) =>
        task.Assignees.Any(a => a.MemberId == userId);

    public static bool CanView(TaskItem task, ICurrentUser user)
    {
        var userId = user.Id ?? throw new UnauthorizedException();
        var role = user.Role ?? throw new UnauthorizedException();

        if (IsOrg(role))
        {
            return true;
        }

        if (task.CreatedById == userId || IsAssignee(task, userId))
        {
            return true;
        }

        return IsDeptLeadership(role)
            && task.Scope == TaskScope.Departmental
            && task.DepartmentId == user.DepartmentId;
    }

    /// <summary>Edit/PUT: Org President/VP — any task; Dept President/VP — only their own department.</summary>
    public static bool CanEdit(TaskItem task, ICurrentUser user)
    {
        var role = user.Role ?? throw new UnauthorizedException();

        if (role is SystemRole.OrgPresident or SystemRole.OrgVicePresident)
        {
            return true;
        }

        if (role is SystemRole.DeptPresident or SystemRole.DeptVicePresident)
        {
            return task.Scope == TaskScope.Departmental && task.DepartmentId == user.DepartmentId;
        }

        return false;
    }

    /// <summary>Cancel shares the same actor set as edit (Org President/VP; Dept President/VP own department).</summary>
    public static bool CanCancel(TaskItem task, ICurrentUser user) => CanEdit(task, user);

    public static void EnsureCanView(TaskItem task, ICurrentUser user)
    {
        if (!CanView(task, user))
        {
            // 404 (not 403) so the caller cannot probe for tasks they may not see.
            throw new NotFoundException("Task", task.Id);
        }
    }

    public static void EnsureCanEdit(TaskItem task, ICurrentUser user)
    {
        EnsureCanView(task, user);
        if (!CanEdit(task, user))
        {
            throw new ForbiddenException("You do not have permission to modify this task.");
        }
    }

    public static void EnsureCanCancel(TaskItem task, ICurrentUser user)
    {
        EnsureCanView(task, user);
        if (!CanCancel(task, user))
        {
            throw new ForbiddenException("You do not have permission to cancel this task.");
        }
    }
}
