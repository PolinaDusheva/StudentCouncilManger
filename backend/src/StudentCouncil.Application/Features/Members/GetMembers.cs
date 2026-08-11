using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Models;

namespace StudentCouncil.Application.Features.Members;

public sealed record GetMembersQuery(
    string? Department = null,
    string? Role = null,
    string? Status = null,
    string? Search = null,
    string? Sort = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<MemberSummaryDto>>;

public sealed class GetMembersHandler : IRequestHandler<GetMembersQuery, PagedResult<MemberSummaryDto>>
{
    private const int MaxPageSize = 100;

    private readonly IMemberDirectory _members;
    private readonly IAppDbContext _db;

    public GetMembersHandler(IMemberDirectory members, IAppDbContext db)
    {
        _members = members;
        _db = db;
    }

    public async Task<PagedResult<MemberSummaryDto>> Handle(GetMembersQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize < 1 ? 20 : request.PageSize, 1, MaxPageSize);

        var query = _members.Members;

        // Status filter (default: Active only).
        var status = Enum.TryParse<MemberStatus>(request.Status, ignoreCase: true, out var parsedStatus)
            ? parsedStatus
            : MemberStatus.Active;
        query = query.Where(m => m.Status == status);

        // Department filter by code (resolved to id to keep the predicate on a direct column).
        if (Enum.TryParse<DepartmentCode>(request.Department, ignoreCase: true, out var code))
        {
            var departmentId = await _db.Departments
                .Where(d => d.Code == code)
                .Select(d => (Guid?)d.Id)
                .FirstOrDefaultAsync(cancellationToken);
            query = query.Where(m => m.DepartmentId == departmentId);
        }

        if (Enum.TryParse<SystemRole>(request.Role, ignoreCase: true, out var role))
        {
            query = query.Where(m => m.Role == role);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(m =>
                m.FullName.ToLower().Contains(term) || m.Email.ToLower().Contains(term));
        }

        query = ApplySort(query, request.Sort);

        var totalCount = await query.CountAsync(cancellationToken);
        var views = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = views.Select(v => v.ToSummary()).ToList();
        return new PagedResult<MemberSummaryDto>(items, page, pageSize, totalCount);
    }

    private static IQueryable<MemberView> ApplySort(IQueryable<MemberView> query, string? sort)
    {
        var descending = sort?.StartsWith('-') == true;
        var field = sort?.TrimStart('-').ToLowerInvariant();

        return field switch
        {
            "joinedon" => descending ? query.OrderByDescending(m => m.JoinedOn) : query.OrderBy(m => m.JoinedOn),
            "role" => descending ? query.OrderByDescending(m => m.Role) : query.OrderBy(m => m.Role),
            _ => descending ? query.OrderByDescending(m => m.FullName) : query.OrderBy(m => m.FullName)
        };
    }
}
