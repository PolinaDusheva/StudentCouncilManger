using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Common.Security;

namespace StudentCouncil.Application.Features.Tasks.Comments;

public sealed record GetTaskCommentsQuery(Guid TaskId) : IRequest<IReadOnlyList<TaskCommentDto>>;

public sealed class GetTaskCommentsHandler : IRequestHandler<GetTaskCommentsQuery, IReadOnlyList<TaskCommentDto>>
{
    private readonly IAppDbContext _db;
    private readonly IMemberDirectory _members;
    private readonly ICurrentUser _currentUser;

    public GetTaskCommentsHandler(IAppDbContext db, IMemberDirectory members, ICurrentUser currentUser)
    {
        _db = db;
        _members = members;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<TaskCommentDto>> Handle(GetTaskCommentsQuery request, CancellationToken cancellationToken)
    {
        var task = await _db.Tasks
            .AsNoTracking()
            .Include(t => t.Assignees)
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);

        TaskAccess.EnsureCanView(task, _currentUser);

        var comments = await _db.TaskComments
            .AsNoTracking()
            .Where(c => c.TaskItemId == request.TaskId)
            .OrderBy(c => c.CreatedAtUtc)
            .Select(c => new { c.Id, c.AuthorId, c.Text, c.CreatedAtUtc })
            .ToListAsync(cancellationToken);

        var authors = await TaskMemberLookup.LoadAsync(_members, comments.Select(c => c.AuthorId), cancellationToken);

        return comments
            .Select(c => new TaskCommentDto(c.Id, authors.Find(c.AuthorId), c.Text, c.CreatedAtUtc))
            .ToList();
    }
}
