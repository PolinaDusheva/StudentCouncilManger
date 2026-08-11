using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Common.Files;
using StudentCouncil.Application.Common.Security;

namespace StudentCouncil.Application.Features.Tasks.Documents;

public sealed record DownloadTaskDocumentQuery(Guid TaskId, Guid DocumentId) : IRequest<StreamedFile>;

public sealed class DownloadTaskDocumentHandler : IRequestHandler<DownloadTaskDocumentQuery, StreamedFile>
{
    private readonly IAppDbContext _db;
    private readonly IFileStorage _storage;
    private readonly ICurrentUser _currentUser;

    public DownloadTaskDocumentHandler(IAppDbContext db, IFileStorage storage, ICurrentUser currentUser)
    {
        _db = db;
        _storage = storage;
        _currentUser = currentUser;
    }

    public async Task<StreamedFile> Handle(DownloadTaskDocumentQuery request, CancellationToken cancellationToken)
    {
        var task = await _db.Tasks
            .AsNoTracking()
            .Include(t => t.Assignees)
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);

        TaskAccess.EnsureCanView(task, _currentUser);

        var document = await _db.TaskDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.TaskItemId == request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Document", request.DocumentId);

        // Local provider streams through the API; an Azure provider could hand back a short-lived SAS URL.
        var stream = await _storage.OpenReadAsync(document.StorageKey, cancellationToken);
        return new StreamedFile(stream, document.ContentType, document.OriginalFileName);
    }
}
