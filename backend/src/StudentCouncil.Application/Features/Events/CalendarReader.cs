using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Calendar;
using StudentCouncil.Application.Common.Members;
using StudentCouncil.Application.Common.Security;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Application.Features.Events;

/// <summary>
/// Produces the merged calendar view for a window: real events (with recurrences expanded) plus
/// virtual deadlines from the caller's <em>visible</em> tasks (decisions #3/#4). Shared by
/// <c>GET /events</c> and <c>GET /events/export.ics</c> so both stay perfectly in sync.
/// </summary>
internal static class CalendarReader
{
    public static async Task<IReadOnlyList<EventDto>> ReadAsync(
        IAppDbContext db,
        IMemberDirectory members,
        ICurrentUser currentUser,
        DateTime windowFrom,
        DateTime windowTo,
        string? type,
        string? department,
        CancellationToken cancellationToken)
    {
        var typeFilter = Enum.TryParse<EventType>(type, ignoreCase: true, out var parsedType)
            ? parsedType
            : (EventType?)null;

        Guid? departmentFilter = null;
        if (Enum.TryParse<DepartmentCode>(department, ignoreCase: true, out var code))
        {
            departmentFilter = await db.Departments
                .Where(d => d.Code == code)
                .Select(d => (Guid?)d.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var result = new List<EventDto>();
        await AddEventsAsync(db, members, windowFrom, windowTo, typeFilter, departmentFilter, result, cancellationToken);

        // Task deadlines are virtual Deadline-typed entries; they only make sense when the type
        // filter is absent or explicitly Deadline.
        if (typeFilter is null or EventType.Deadline)
        {
            await AddDeadlinesAsync(db, currentUser, windowFrom, windowTo, departmentFilter, result, cancellationToken);
        }

        return result.OrderBy(e => e.StartUtc).ToList();
    }

    private static async Task AddEventsAsync(
        IAppDbContext db, IMemberDirectory members, DateTime windowFrom, DateTime windowTo,
        EventType? typeFilter, Guid? departmentFilter, List<EventDto> result, CancellationToken cancellationToken)
    {
        // One-off events must overlap the window; recurring ones only need to have started before it
        // ends (the expander decides which occurrences actually fall inside).
        var query = db.CalendarEvents
            .AsNoTracking()
            .Include(e => e.Department)
            .Where(e =>
                (e.Recurrence == RecurrenceType.None && e.StartUtc < windowTo && e.EndUtc > windowFrom)
                || (e.Recurrence != RecurrenceType.None && e.StartUtc < windowTo));

        if (typeFilter is { } eventType)
        {
            query = query.Where(e => e.Type == eventType);
        }

        if (departmentFilter is { } departmentId)
        {
            query = query.Where(e => e.DepartmentId == departmentId);
        }

        var events = await query.ToListAsync(cancellationToken);

        var organizerMap = await MemberLookup.LoadAsync(members, events.Select(e => e.OrganizerId), cancellationToken);

        foreach (var calendarEvent in events)
        {
            var organizer = organizerMap.Find(calendarEvent.OrganizerId);
            var isRecurring = calendarEvent.Recurrence != RecurrenceType.None;

            foreach (var (startUtc, endUtc) in RecurrenceExpander.Expand(calendarEvent, windowFrom, windowTo))
            {
                result.Add(EventMappings.ToDto(calendarEvent, organizer, startUtc, endUtc, isRecurring));
            }
        }
    }

    private static async Task AddDeadlinesAsync(
        IAppDbContext db, ICurrentUser currentUser, DateTime windowFrom, DateTime windowTo,
        Guid? departmentFilter, List<EventDto> result, CancellationToken cancellationToken)
    {
        // Reuse Phase 3 visibility so a deadline of a hidden task never leaks (decision #4).
        var tasks = TaskAccess.Visible(db.Tasks.AsNoTracking().Include(t => t.Department), currentUser)
            .Where(t => t.DueAtUtc != null && t.DueAtUtc >= windowFrom && t.DueAtUtc < windowTo);

        if (departmentFilter is { } departmentId)
        {
            tasks = tasks.Where(t => t.DepartmentId == departmentId);
        }

        var deadlines = await tasks
            .Select(t => new
            {
                t.Id,
                t.Title,
                DueAtUtc = t.DueAtUtc!.Value,
                Department = t.Department != null ? (DepartmentCode?)t.Department.Code : null
            })
            .ToListAsync(cancellationToken);

        foreach (var deadline in deadlines)
        {
            result.Add(EventMappings.Deadline(deadline.Id, deadline.Title, deadline.DueAtUtc, deadline.Department));
        }
    }
}
