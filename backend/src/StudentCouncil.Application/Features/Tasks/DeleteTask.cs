using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Audit;
using StudentCouncil.Application.Common.Exceptions;

namespace StudentCouncil.Application.Features.Tasks;

/// <summary>
/// Physically deletes a task (spec 7.4, OrgPresident only). <see cref="TaskItem"/> is soft-deletable
/// and the audit interceptor would otherwise convert this to a soft-delete, so we issue an
/// <c>ExecuteDelete</c> that bypasses the interceptor; the database cascade removes assignees,
/// comments and document rows, and we delete the document blobs first (decision in plan §10).
/// </summary>
public sealed record DeleteTaskCommand(Guid Id) : IRequest;

public sealed class DeleteTaskHandler : IRequestHandler<DeleteTaskCommand>
{
    private readonly IAppDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IAuditRecorder _audit;

    public DeleteTaskHandler(IAppDbContext db, IFileStorage storage, IAuditRecorder audit)
    {
        _db = db;
        _storage = storage;
        _audit = audit;
    }

    public async Task Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var storageKeys = await _db.TaskDocuments
            .AsNoTracking()
            .Where(d => d.TaskItemId == request.Id)
            .Select(d => d.StorageKey)
            .ToListAsync(cancellationToken);

        // Capture the audit details while the task still exists (decision #4).
        var task = await _db.Tasks
            .AsNoTracking()
            .Where(t => t.Id == request.Id)
            .Select(t => new { t.Title, t.Scope, t.DepartmentId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Task", request.Id);

        // Remove the blobs before the rows so nothing is orphaned in storage.
        foreach (var key in storageKeys)
        {
            await _storage.DeleteAsync(key, cancellationToken);
        }

        // True physical delete (bypasses the soft-delete interceptor); DB cascade removes children.
        await _db.Tasks.Where(t => t.Id == request.Id).ExecuteDeleteAsync(cancellationToken);

        _audit.Record(AuditActions.TaskDeleted, AuditEntities.TaskItem, request.Id,
            new { task.Title, task.Scope, task.DepartmentId });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
