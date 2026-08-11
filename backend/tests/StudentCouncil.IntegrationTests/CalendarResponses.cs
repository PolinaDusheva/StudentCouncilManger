namespace StudentCouncil.IntegrationTests;

// Response shapes for the events/duties/budget endpoints (enums serialised as strings).
// MemberSummaryResponse is reused from TaskResponses.cs.

public sealed record EventResponse(
    Guid Id, string Title, string? Description, DateTime StartUtc, DateTime EndUtc, string? Location,
    string Type, string? Department, MemberSummaryResponse? Organizer, string Recurrence,
    bool IsDeadline, Guid? TaskId, DateTime? OccurrenceStartUtc);

public sealed record EventDetailResponse(
    Guid Id, string Title, string? Description, DateTime StartUtc, DateTime EndUtc, string? Location,
    string Type, string? Department, MemberSummaryResponse? Organizer, string Recurrence,
    bool IsDeadline, Guid? TaskId, DateTime? OccurrenceStartUtc, List<MemberSummaryResponse> Participants);

public sealed record EventConflictResponse(Guid Id, string Title, DateTime StartUtc, DateTime EndUtc);

public sealed record EventMutationResponse(EventDetailResponse Event, List<EventConflictResponse> ConflictsWith);

public sealed record DutyRecordResponse(
    Guid Id, MemberSummaryResponse? Member, DateTime StartUtc, DateTime EndUtc, int DurationMinutes,
    int PeriodYear, int PeriodMonth, MemberSummaryResponse? RecordedBy, string? Note);

public sealed record DutySummaryRowResponse(
    MemberSummaryResponse Member, int TotalMinutes, int RequiredMinutes, bool MetNorm);

public sealed record MyDutySummaryResponse(int Year, int Month, int TotalMinutes, int RequiredMinutes, bool MetNorm);

public sealed record RemindResponse(int Notified);

public sealed record ExpenseResponse(
    Guid Id, string Description, decimal AmountEur, DateOnly SpentOn,
    MemberSummaryResponse? AddedBy, DateTime CreatedAtUtc);

public sealed record BudgetSummaryResponse(int Year, decimal TotalEur);

public sealed record PagedExpensesResponse(
    List<ExpenseResponse> Items, int Page, int PageSize, int TotalCount, int TotalPages);
