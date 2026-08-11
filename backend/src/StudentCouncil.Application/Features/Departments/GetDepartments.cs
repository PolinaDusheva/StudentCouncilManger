using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;

namespace StudentCouncil.Application.Features.Departments;

public sealed record GetDepartmentsQuery : IRequest<IReadOnlyList<DepartmentDto>>;

public sealed class GetDepartmentsHandler : IRequestHandler<GetDepartmentsQuery, IReadOnlyList<DepartmentDto>>
{
    private readonly IAppDbContext _db;
    private readonly IMemberDirectory _members;

    public GetDepartmentsHandler(IAppDbContext db, IMemberDirectory members)
    {
        _db = db;
        _members = members;
    }

    public async Task<IReadOnlyList<DepartmentDto>> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken)
    {
        var departments = await _db.Departments
            .OrderBy(d => d.Code)
            .ToListAsync(cancellationToken);

        var membersByDepartment = (await _members.Members
                .Where(m => m.Status == MemberStatus.Active && m.DepartmentId != null)
                .ToListAsync(cancellationToken))
            .GroupBy(m => m.DepartmentId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        return departments
            .Select(d =>
            {
                var members = membersByDepartment.GetValueOrDefault(d.Id, []);
                return new DepartmentDto(
                    d.Id, d.Code, d.Name, d.Description,
                    members.Count, DepartmentMappings.Leadership(members));
            })
            .ToList();
    }
}
