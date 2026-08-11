using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Common.Options;

namespace StudentCouncil.Application.Features.Duties;

public sealed record GetMyDutySummaryQuery(int? Year = null, int? Month = null) : IRequest<MyDutySummaryDto>;

public sealed class GetMyDutySummaryHandler : IRequestHandler<GetMyDutySummaryQuery, MyDutySummaryDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;
    private readonly IOptions<DutyPolicyOptions> _dutyPolicy;

    public GetMyDutySummaryHandler(
        IAppDbContext db, ICurrentUser currentUser, IDateTime clock, IOptions<DutyPolicyOptions> dutyPolicy)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _dutyPolicy = dutyPolicy;
    }

    public async Task<MyDutySummaryDto> Handle(GetMyDutySummaryQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.Id ?? throw new UnauthorizedException();
        var now = _clock.UtcNow;
        var year = request.Year ?? now.Year;
        var month = request.Month ?? now.Month;
        var required = _dutyPolicy.Value.RequiredMinutesPerMonth;

        var total = await _db.DutyRecords
            .AsNoTracking()
            .Where(d => d.MemberId == userId && d.PeriodYear == year && d.PeriodMonth == month)
            .Select(d => d.DurationMinutes)
            .SumAsync(cancellationToken);

        return new MyDutySummaryDto(year, month, total, required, total >= required);
    }
}
