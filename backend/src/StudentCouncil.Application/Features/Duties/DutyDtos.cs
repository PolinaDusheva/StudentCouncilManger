using StudentCouncil.Application.Features.Members;

namespace StudentCouncil.Application.Features.Duties;

/// <summary>A single recorded duty shift.</summary>
public sealed record DutyRecordDto(
    Guid Id,
    MemberSummaryDto? Member,
    DateTime StartUtc,
    DateTime EndUtc,
    int DurationMinutes,
    int PeriodYear,
    int PeriodMonth,
    MemberSummaryDto? RecordedBy,
    string? Note);

/// <summary>One row of the monthly norm table: a member's total against the required minutes.</summary>
public sealed record DutySummaryDto(
    MemberSummaryDto Member,
    int TotalMinutes,
    int RequiredMinutes,
    bool MetNorm);

/// <summary>The caller's own monthly duty standing.</summary>
public sealed record MyDutySummaryDto(
    int Year,
    int Month,
    int TotalMinutes,
    int RequiredMinutes,
    bool MetNorm);

/// <summary>How many recipients a duty reminder was dispatched to.</summary>
public sealed record RemindResult(int Notified);
