using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Members;
using StudentCouncil.Application.Common.Models;

namespace StudentCouncil.Application.Features.Budget;

public sealed record GetExpensesQuery(
    int? Year = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<ExpenseDto>>;

public sealed class GetExpensesHandler : IRequestHandler<GetExpensesQuery, PagedResult<ExpenseDto>>
{
    private const int MaxPageSize = 100;

    private readonly IAppDbContext _db;
    private readonly IMemberDirectory _members;

    public GetExpensesHandler(IAppDbContext db, IMemberDirectory members)
    {
        _db = db;
        _members = members;
    }

    public async Task<PagedResult<ExpenseDto>> Handle(GetExpensesQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize < 1 ? 20 : request.PageSize, 1, MaxPageSize);

        var query = _db.Expenses.AsNoTracking();
        if (request.Year is { } year)
        {
            query = query.Where(e => e.Year == year);
        }

        // Chronological, most recent spend first; CreatedAtUtc breaks ties deterministically.
        query = query.OrderByDescending(e => e.SpentOn).ThenByDescending(e => e.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var expenses = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var map = await MemberLookup.LoadAsync(_members, expenses.Select(e => e.AddedById), cancellationToken);
        var items = expenses.Select(e => ExpenseMappings.Build(e, map)).ToList();

        return new PagedResult<ExpenseDto>(items, page, pageSize, totalCount);
    }
}
