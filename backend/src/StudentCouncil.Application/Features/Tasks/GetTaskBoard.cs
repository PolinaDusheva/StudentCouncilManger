using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Security;

namespace StudentCouncil.Application.Features.Tasks;

/// <summary>Visible tasks arranged into the four Kanban columns (Cancelled is excluded — functional 6.6).</summary>
public sealed record GetTaskBoardQuery : IRequest<TaskBoardDto>;

public sealed class GetTaskBoardHandler : IRequestHandler<GetTaskBoardQuery, TaskBoardDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;

    public GetTaskBoardHandler(IAppDbContext db, ICurrentUser currentUser, IDateTime clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<TaskBoardDto> Handle(GetTaskBoardQuery request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        var items = await TaskAccess.Visible(_db.Tasks.AsNoTracking(), _currentUser)
            .Where(t => t.Status != TaskStatus.Cancelled)
            .OrderBy(t => t.DueAtUtc)
            .Select(TaskMappings.ToListItem(now))
            .ToListAsync(cancellationToken);

        IReadOnlyList<TaskListItemDto> Column(TaskStatus status) =>
            items.Where(i => i.Status == status).ToList();

        return new TaskBoardDto(new TaskBoardColumns(
            New: Column(TaskStatus.New),
            InProgress: Column(TaskStatus.InProgress),
            InReview: Column(TaskStatus.InReview),
            Completed: Column(TaskStatus.Completed)));
    }
}
