using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Common.Security;

namespace StudentCouncil.Application.Features.Tasks;

/// <summary>Marks a task Cancelled. Idempotent — cancelling an already-cancelled task is a no-op.</summary>
public sealed record CancelTaskCommand(Guid Id) : IRequest;

public sealed class CancelTaskHandler : IRequestHandler<CancelTaskCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CancelTaskHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(CancelTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _db.Tasks
            .Include(t => t.Assignees)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Task", request.Id);

        TaskAccess.EnsureCanCancel(task, _currentUser);

        if (task.Status == TaskStatus.Cancelled)
        {
            return;
        }

        task.Status = TaskStatus.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
