using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Common.Files;
using StudentCouncil.Application.Common.Security;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Application.Features.Tasks.Documents;

public sealed record UploadTaskDocumentCommand(Guid TaskId, FileUpload File) : IRequest<TaskDocumentDto>;

public sealed class UploadTaskDocumentHandler : IRequestHandler<UploadTaskDocumentCommand, TaskDocumentDto>
{
    private readonly IAppDbContext _db;
    private readonly IMemberDirectory _members;
    private readonly IFileStorage _storage;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;
    private readonly StorageLimits _limits;

    public UploadTaskDocumentHandler(
        IAppDbContext db,
        IMemberDirectory members,
        IFileStorage storage,
        ICurrentUser currentUser,
        IDateTime clock,
        StorageLimits limits)
    {
        _db = db;
        _members = members;
        _storage = storage;
        _currentUser = currentUser;
        _clock = clock;
        _limits = limits;
    }

    public async Task<TaskDocumentDto> Handle(UploadTaskDocumentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.Id ?? throw new UnauthorizedException();

        var task = await _db.Tasks
            .AsNoTracking()
            .Include(t => t.Assignees)
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);

        // Anyone who can see the task can attach a document (functional 7.3).
        TaskAccess.EnsureCanView(task, _currentUser);

        var file = request.File;
        var header = await FileBytes.ReadHeaderAsync(file.Content, FileSignatures.HeaderLength, cancellationToken);
        var extension = FileValidation.Validate(
            file.FileName, file.ContentType, file.Length, header, _limits.MaxDocumentBytes, AllowedFileTypes.Documents);

        var contentType = AllowedFileTypes.Documents[extension][0];
        var key = StorageKeys.TaskDocument(request.TaskId, extension);
        await _storage.SaveAsync(file.Content, key, contentType, cancellationToken);

        var document = new TaskDocument
        {
            Id = Guid.NewGuid(),
            TaskItemId = request.TaskId,
            OriginalFileName = Path.GetFileName(file.FileName),
            StorageKey = key,
            ContentType = contentType,
            SizeBytes = file.Length,
            UploadedById = userId,
            UploadedAtUtc = _clock.UtcNow
        };

        _db.TaskDocuments.Add(document);
        await _db.SaveChangesAsync(cancellationToken);

        var uploaders = await TaskMemberLookup.LoadAsync(_members, [userId], cancellationToken);
        return new TaskDocumentDto(
            document.Id, document.OriginalFileName, document.ContentType, document.SizeBytes,
            uploaders.Find(userId), document.UploadedAtUtc);
    }
}
