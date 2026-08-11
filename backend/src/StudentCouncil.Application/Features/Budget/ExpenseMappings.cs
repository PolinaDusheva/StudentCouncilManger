using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Members;
using StudentCouncil.Application.Features.Members;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Application.Features.Budget;

public static class ExpenseMappings
{
    public static ExpenseDto Build(Expense expense, IReadOnlyDictionary<Guid, MemberSummaryDto> map) =>
        new(
            expense.Id,
            expense.Description,
            expense.AmountEur,
            expense.SpentOn,
            map.Find(expense.AddedById),
            expense.CreatedAtUtc);

    /// <summary>Projects a single expense, batch-resolving its author summary.</summary>
    public static async Task<ExpenseDto> ToDtoAsync(
        IMemberDirectory members, Expense expense, CancellationToken cancellationToken)
    {
        var map = await MemberLookup.LoadAsync(members, [expense.AddedById], cancellationToken);
        return Build(expense, map);
    }
}
