using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Common.Security;

namespace StudentCouncil.Application.Features.Tasks.Documents;

public sealed record GetTaskDocumentsQuery(Guid TaskId) : IRequest<IReadOnlyList<TaskDocumentDto>>;

public sealed class GetTaskDocumentsHandler : IRequestHandler<GetTaskDocumentsQuery, IReadOnlyList<TaskDocumentDto>>
{
    private readonly IAppDbContext _db;
    private readonly IMemberDirectory _members;
    private readonly ICurrentUser _currentUser;

    public GetTaskDocumentsHandler(IAppDbContext db, IMemberDirectory members, ICurrentUser currentUser)
    {
        _db = db;
        _members = members;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<TaskDocumentDto>> Handle(GetTaskDocumentsQuery request, CancellationToken cancellationToken)
    {
        var task = await _db.Tasks
            .AsNoTracking()
            .Include(t => t.Assignees)
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);

        TaskAccess.EnsureCanView(task, _currentUser);

        var documents = await _db.TaskDocuments
            .AsNoTracking()
            .Where(d => d.TaskItemId == request.TaskId)
            .OrderBy(d => d.UploadedAtUtc)
            .Select(d => new { d.Id, d.OriginalFileName, d.ContentType, d.SizeBytes, d.UploadedById, d.UploadedAtUtc })
            .ToListAsync(cancellationToken);

        var uploaders = await TaskMemberLookup.LoadAsync(_members, documents.Select(d => d.UploadedById), cancellationToken);

        return documents
            .Select(d => new TaskDocumentDto(
                d.Id, d.OriginalFileName, d.ContentType, d.SizeBytes, uploaders.Find(d.UploadedById), d.UploadedAtUtc))
            .ToList();
    }
}
