using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;

namespace StudentCouncil.Application.Features.Budget;

public sealed record GetBudgetSummaryQuery(int? Year = null) : IRequest<BudgetSummaryDto>;

public sealed class GetBudgetSummaryHandler : IRequestHandler<GetBudgetSummaryQuery, BudgetSummaryDto>
{
    private readonly IAppDbContext _db;
    private readonly IDateTime _clock;

    public GetBudgetSummaryHandler(IAppDbContext db, IDateTime clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<BudgetSummaryDto> Handle(GetBudgetSummaryQuery request, CancellationToken cancellationToken)
    {
        var year = request.Year ?? _clock.UtcNow.Year;

        var total = await _db.Expenses
            .AsNoTracking()
            .Where(e => e.Year == year)
            .Select(e => e.AmountEur)
            .SumAsync(cancellationToken);

        return new BudgetSummaryDto(year, total);
    }
}
