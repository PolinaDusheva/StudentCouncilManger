using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Common.Security;

namespace StudentCouncil.Application.Features.Tasks;

public sealed record GetTaskQuery(Guid Id) : IRequest<TaskDetailDto>;

public sealed class GetTaskHandler : IRequestHandler<GetTaskQuery, TaskDetailDto>
{
    private readonly IAppDbContext _db;
    private readonly IMemberDirectory _members;
    private readonly ICurrentUser _currentUser;

    public GetTaskHandler(IAppDbContext db, IMemberDirectory members, ICurrentUser currentUser)
    {
        _db = db;
        _members = members;
        _currentUser = currentUser;
    }

    public async Task<TaskDetailDto> Handle(GetTaskQuery request, CancellationToken cancellationToken)
    {
        var task = await _db.Tasks
            .AsNoTracking()
            .Include(t => t.Department)
            .Include(t => t.Assignees)
            .Include(t => t.Comments)
            .Include(t => t.Documents)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Task", request.Id);

        // Visibility (invisible -> 404, not 403).
        TaskAccess.EnsureCanView(task, _currentUser);

        return await TaskDetailBuilder.BuildAsync(task, _members, cancellationToken);
    }
}
