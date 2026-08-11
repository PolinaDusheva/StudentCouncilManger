using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Common.Members;
using StudentCouncil.Application.Features.Members;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Application.Features.Events;

public static class EventMappings
{
    /// <summary>Maps one (possibly expanded) occurrence of a real event to a list DTO.</summary>
    public static EventDto ToDto(
        CalendarEvent calendarEvent, MemberSummaryDto? organizer,
        DateTime startUtc, DateTime endUtc, bool isRecurringOccurrence) =>
        new(
            calendarEvent.Id,
            calendarEvent.Title,
            calendarEvent.Description,
            startUtc,
            endUtc,
            calendarEvent.Location,
            calendarEvent.Type,
            calendarEvent.Department?.Code,
            organizer,
            calendarEvent.Recurrence,
            IsDeadline: false,
            TaskId: null,
            // Marks the row as a virtual recurring instance; null for one-off events.
            OccurrenceStartUtc: isRecurringOccurrence ? startUtc : null);

    /// <summary>Maps a task's due date to a read-only virtual <c>Deadline</c> entry (decision #4).</summary>
    public static EventDto Deadline(Guid taskId, string title, DateTime dueAtUtc, DepartmentCode? department) =>
        new(
            taskId,
            title,
            Description: null,
            dueAtUtc,
            dueAtUtc,
            Location: null,
            EventType.Deadline,
            department,
            Organizer: null,
            RecurrenceType.None,
            IsDeadline: true,
            TaskId: taskId,
            OccurrenceStartUtc: null);
}

/// <summary>Builds the detail view (organiser + participants) for a single base event.</summary>
internal static class EventDetailBuilder
{
    public static async Task<EventDetailDto> LoadAndBuildAsync(
        IAppDbContext db, IMemberDirectory members, Guid eventId, CancellationToken cancellationToken)
    {
        var calendarEvent = await db.CalendarEvents
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Participants)
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken)
            ?? throw new NotFoundException("Event", eventId);

        return await BuildAsync(calendarEvent, members, cancellationToken);
    }

    public static async Task<EventDetailDto> BuildAsync(
        CalendarEvent calendarEvent, IMemberDirectory members, CancellationToken cancellationToken)
    {
        var ids = new List<Guid> { calendarEvent.OrganizerId };
        ids.AddRange(calendarEvent.Participants.Select(p => p.MemberId));

        var map = await MemberLookup.LoadAsync(members, ids, cancellationToken);

        var participants = calendarEvent.Participants
            .Select(p => map.Find(p.MemberId))
            .OfType<MemberSummaryDto>()
            .ToList();

        return new EventDetailDto(
            calendarEvent.Id,
            calendarEvent.Title,
            calendarEvent.Description,
            calendarEvent.StartUtc,
            calendarEvent.EndUtc,
            calendarEvent.Location,
            calendarEvent.Type,
            calendarEvent.Department?.Code,
            map.Find(calendarEvent.OrganizerId),
            calendarEvent.Recurrence,
            IsDeadline: false,
            TaskId: null,
            OccurrenceStartUtc: null,
            participants);
    }
}

/// <summary>Non-blocking schedule-overlap detection shared by create and update (decision #5).</summary>
internal static class EventConflicts
{
    public static async Task<IReadOnlyList<EventConflictDto>> FindAsync(
        IAppDbContext db, Guid eventId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken) =>
        await db.CalendarEvents
            .AsNoTracking()
            .Where(e => e.Id != eventId && e.StartUtc < endUtc && e.EndUtc > startUtc)
            .OrderBy(e => e.StartUtc)
            .Select(e => new EventConflictDto(e.Id, e.Title, e.StartUtc, e.EndUtc))
            .ToListAsync(cancellationToken);
}
