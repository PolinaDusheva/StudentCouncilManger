using StudentCouncil.Application.Features.Members;

namespace StudentCouncil.Application.Features.Budget;

/// <summary>A single recorded expense.</summary>
public sealed record ExpenseDto(
    Guid Id,
    string Description,
    decimal AmountEur,
    DateOnly SpentOn,
    MemberSummaryDto? AddedBy,
    DateTime CreatedAtUtc);

/// <summary>Total spend for a calendar year.</summary>
public sealed record BudgetSummaryDto(int Year, decimal TotalEur);
