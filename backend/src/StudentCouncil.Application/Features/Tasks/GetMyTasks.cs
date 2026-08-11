using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;

namespace StudentCouncil.Application.Features.Tasks;

/// <summary>Tasks the caller is assigned to (a subset of what they can see).</summary>
public sealed record GetMyTasksQuery : IRequest<IReadOnlyList<TaskListItemDto>>;

public sealed class GetMyTasksHandler : IRequestHandler<GetMyTasksQuery, IReadOnlyList<TaskListItemDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;

    public GetMyTasksHandler(IAppDbContext db, ICurrentUser currentUser, IDateTime clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<IReadOnlyList<TaskListItemDto>> Handle(GetMyTasksQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.Id ?? throw new UnauthorizedException();
        var now = _clock.UtcNow;

        return await _db.Tasks
            .AsNoTracking()
            .Where(t => t.Assignees.Any(a => a.MemberId == userId))
            .OrderBy(t => t.Status)
            .ThenBy(t => t.DueAtUtc)
            .Select(TaskMappings.ToListItem(now))
            .ToListAsync(cancellationToken);
    }
}
