using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Options;
using StudentCouncil.Application.Features.Members;

namespace StudentCouncil.Application.Features.Duties;

public sealed record GetDutySummaryQuery(int? Year = null, int? Month = null) : IRequest<IReadOnlyList<DutySummaryDto>>;

public sealed class GetDutySummaryHandler : IRequestHandler<GetDutySummaryQuery, IReadOnlyList<DutySummaryDto>>
{
    private readonly IAppDbContext _db;
    private readonly IMemberDirectory _members;
    private readonly IDateTime _clock;
    private readonly IOptions<DutyPolicyOptions> _dutyPolicy;

    public GetDutySummaryHandler(
        IAppDbContext db, IMemberDirectory members, IDateTime clock, IOptions<DutyPolicyOptions> dutyPolicy)
    {
        _db = db;
        _members = members;
        _clock = clock;
        // Read lazily here (not captured at registration) so the integration-test host can override it.
        _dutyPolicy = dutyPolicy;
    }

    public async Task<IReadOnlyList<DutySummaryDto>> Handle(GetDutySummaryQuery request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var year = request.Year ?? now.Year;
        var month = request.Month ?? now.Month;
        var required = _dutyPolicy.Value.RequiredMinutesPerMonth;

        // Every active member appears — including those with no records this month, so the table can
        // flag them as under norm (decision #8).
        var activeMembers = await _members.Members
            .Where(m => m.Status == MemberStatus.Active)
            .ToListAsync(cancellationToken);

        var totals = await DutyTotals.ByMemberAsync(_db, year, month, cancellationToken);

        return activeMembers
            .Select(member =>
            {
                var total = totals.GetValueOrDefault(member.Id, 0);
                return new DutySummaryDto(member.ToSummary(), total, required, total >= required);
            })
            // Under-norm members first (so the table leads with who needs chasing), then by name.
            .OrderBy(s => s.MetNorm)
            .ThenBy(s => s.Member.FullName)
            .ToList();
    }
}
