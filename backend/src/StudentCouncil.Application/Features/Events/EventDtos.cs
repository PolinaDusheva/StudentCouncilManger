using StudentCouncil.Application.Features.Members;

namespace StudentCouncil.Application.Features.Events;

/// <summary>
/// A calendar entry returned by the list/export endpoints. Real events, expanded recurring
/// occurrences and virtual task deadlines all share this shape; the flags disambiguate them
/// (<see cref="IsDeadline"/>/<see cref="TaskId"/> for deadlines, <see cref="OccurrenceStartUtc"/>
/// for recurring instances).
/// </summary>
public sealed record EventDto(
    Guid Id,
    string Title,
    string? Description,
    DateTime StartUtc,
    DateTime EndUtc,
    string? Location,
    EventType Type,
    DepartmentCode? Department,
    MemberSummaryDto? Organizer,
    RecurrenceType Recurrence,
    bool IsDeadline,
    Guid? TaskId,
    DateTime? OccurrenceStartUtc);

/// <summary>Full event view: the base event plus its participants.</summary>
public sealed record EventDetailDto(
    Guid Id,
    string Title,
    string? Description,
    DateTime StartUtc,
    DateTime EndUtc,
    string? Location,
    EventType Type,
    DepartmentCode? Department,
    MemberSummaryDto? Organizer,
    RecurrenceType Recurrence,
    bool IsDeadline,
    Guid? TaskId,
    DateTime? OccurrenceStartUtc,
    IReadOnlyList<MemberSummaryDto> Participants);

/// <summary>A schedule overlap surfaced as a non-blocking warning (decision #5).</summary>
public sealed record EventConflictDto(Guid Id, string Title, DateTime StartUtc, DateTime EndUtc);

/// <summary>Result of a create/update: the saved event plus any overlapping events (warning only).</summary>
public sealed record EventMutationResult(EventDetailDto Event, IReadOnlyList<EventConflictDto> ConflictsWith);
