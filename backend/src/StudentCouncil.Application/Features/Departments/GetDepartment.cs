using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Features.Members;

namespace StudentCouncil.Application.Features.Departments;

public sealed record GetDepartmentQuery(Guid Id) : IRequest<DepartmentDetailDto>;

public sealed class GetDepartmentHandler : IRequestHandler<GetDepartmentQuery, DepartmentDetailDto>
{
    private readonly IAppDbContext _db;
    private readonly IMemberDirectory _members;

    public GetDepartmentHandler(IAppDbContext db, IMemberDirectory members)
    {
        _db = db;
        _members = members;
    }

    public async Task<DepartmentDetailDto> Handle(GetDepartmentQuery request, CancellationToken cancellationToken)
    {
        var department = await _db.Departments.FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Department", request.Id);

        var members = await _members.Members
            .Where(m => m.DepartmentId == request.Id && m.Status == MemberStatus.Active)
            .OrderBy(m => m.FullName)
            .ToListAsync(cancellationToken);

        return new DepartmentDetailDto(
            department.Id, department.Code, department.Name, department.Description,
            members.Count, DepartmentMappings.Leadership(members),
            members.Select(m => m.ToSummary()).ToList());
    }
}
