using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Audit;
using StudentCouncil.Application.Common.Exceptions;

namespace StudentCouncil.Application.Features.Tasks.Documents;

/// <summary>Deletes a document (uploader or Org leadership) from both storage and the database.</summary>
public sealed record DeleteTaskDocumentCommand(Guid TaskId, Guid DocumentId) : IRequest;

public sealed class DeleteTaskDocumentHandler : IRequestHandler<DeleteTaskDocumentCommand>
{
    private readonly IAppDbContext _db;
    private readonly IFileStorage _storage;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditRecorder _audit;

    public DeleteTaskDocumentHandler(
        IAppDbContext db, IFileStorage storage, ICurrentUser currentUser, IAuditRecorder audit)
    {
        _db = db;
        _storage = storage;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task Handle(DeleteTaskDocumentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.Id ?? throw new UnauthorizedException();
        var role = _currentUser.Role ?? throw new UnauthorizedException();

        var document = await _db.TaskDocuments
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.TaskItemId == request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Document", request.DocumentId);

        var isOrgLeadership = role is SystemRole.OrgPresident or SystemRole.OrgVicePresident;
        if (document.UploadedById != userId && !isOrgLeadership)
        {
            throw new ForbiddenException("Only the uploader or organisation leadership can delete this document.");
        }

        await _storage.DeleteAsync(document.StorageKey, cancellationToken);

        // Recorded before Remove so the details are still readable; committed by the SaveChanges below.
        _audit.Record(AuditActions.DocumentDeleted, AuditEntities.TaskDocument, document.Id,
            new { taskId = document.TaskItemId, document.OriginalFileName });

        _db.TaskDocuments.Remove(document);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
