using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Common.Members;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Application.Features.Duties;

public sealed record GetDutyRecordsQuery(
    Guid? MemberId = null,
    int? Year = null,
    int? Month = null) : IRequest<IReadOnlyList<DutyRecordDto>>;

public sealed class GetDutyRecordsHandler : IRequestHandler<GetDutyRecordsQuery, IReadOnlyList<DutyRecordDto>>
{
    private readonly IAppDbContext _db;
    private readonly IMemberDirectory _members;
    private readonly ICurrentUser _currentUser;

    public GetDutyRecordsHandler(IAppDbContext db, IMemberDirectory members, ICurrentUser currentUser)
    {
        _db = db;
        _members = members;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<DutyRecordDto>> Handle(GetDutyRecordsQuery request, CancellationToken cancellationToken)
    {
        var role = _currentUser.Role ?? throw new UnauthorizedException();
        var userId = _currentUser.Id ?? throw new UnauthorizedException();
        var canManage = role is SystemRole.OrgPresident or SystemRole.OrgVicePresident;

        var query = _db.DutyRecords.AsNoTracking().AsQueryable();

        if (!canManage)
        {
            // Decision #7: anyone who is not duty management sees only their own records; the supplied
            // memberId is ignored (never trust a request id for "own" resources — spec 6.3).
            query = query.Where(d => d.MemberId == userId);
        }
        else if (request.MemberId is { } memberId)
        {
            query = query.Where(d => d.MemberId == memberId);
        }

        if (request.Year is { } year)
        {
            query = query.Where(d => d.PeriodYear == year);
        }

        if (request.Month is { } month)
        {
            query = query.Where(d => d.PeriodMonth == month);
        }

        var records = await query.OrderByDescending(d => d.StartUtc).ToListAsync(cancellationToken);

        var ids = records.Select(d => d.MemberId).Concat(records.Select(d => d.RecordedById));
        var map = await MemberLookup.LoadAsync(_members, ids, cancellationToken);

        return records.Select(d => DutyMappings.Build(d, map)).ToList();
    }
}
