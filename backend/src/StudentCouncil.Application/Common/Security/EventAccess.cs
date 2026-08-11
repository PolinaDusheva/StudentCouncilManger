using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Application.Common.Security;

/// <summary>
/// Edit/delete rules for calendar events (spec 6.3, functional 8.2; decision #2). Unlike tasks there
/// is no visibility dimension — every authenticated member sees every event (decision #1) — so a
/// missing event is always a genuine 404, never a hidden-existence 404, and there is no <c>Visible</c>.
/// </summary>
public static class EventAccess
{
    private static bool IsOrgLeadership(SystemRole role) =>
        role is SystemRole.OrgPresident or SystemRole.OrgVicePresident;

    private static bool IsDeptLeadership(SystemRole role) =>
        role is SystemRole.DeptPresident or SystemRole.DeptVicePresident;

    private static bool IsOwnDepartment(CalendarEvent calendarEvent, ICurrentUser user) =>
        calendarEvent.DepartmentId != null && calendarEvent.DepartmentId == user.DepartmentId;

    /// <summary>
    /// Edit/PUT: the organiser themselves, Org President/VP (any event), or Dept President/VP for an
    /// event organised by their own department. So a secretary may edit an event they created.
    /// </summary>
    public static bool CanEdit(CalendarEvent calendarEvent, ICurrentUser user)
    {
        var userId = user.Id ?? throw new UnauthorizedException();
        var role = user.Role ?? throw new UnauthorizedException();

        if (calendarEvent.OrganizerId == userId || IsOrgLeadership(role))
        {
            return true;
        }

        return IsDeptLeadership(role) && IsOwnDepartment(calendarEvent, user);
    }

    /// <summary>
    /// Delete: Org President/VP, or Dept President/VP for their own department. NOT owner-based — a
    /// secretary who created an event may edit it but must not delete it (functional 8.2 matrix).
    /// </summary>
    public static bool CanDelete(CalendarEvent calendarEvent, ICurrentUser user)
    {
        var role = user.Role ?? throw new UnauthorizedException();

        if (IsOrgLeadership(role))
        {
            return true;
        }

        return IsDeptLeadership(role) && IsOwnDepartment(calendarEvent, user);
    }

    public static void EnsureCanEdit(CalendarEvent calendarEvent, ICurrentUser user)
    {
        if (!CanEdit(calendarEvent, user))
        {
            throw new ForbiddenException("You do not have permission to modify this event.");
        }
    }

    public static void EnsureCanDelete(CalendarEvent calendarEvent, ICurrentUser user)
    {
        if (!CanDelete(calendarEvent, user))
        {
            throw new ForbiddenException("You do not have permission to delete this event.");
        }
    }
}
