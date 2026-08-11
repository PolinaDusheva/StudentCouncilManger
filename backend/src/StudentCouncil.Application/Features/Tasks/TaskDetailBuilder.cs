using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Features.Members;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Application.Features.Tasks;

/// <summary>
/// Assembles a <see cref="TaskDetailDto"/> from a loaded <see cref="TaskItem"/>, batch-resolving the
/// member ids it references (creator, assignees, comment authors, document uploaders).
/// </summary>
internal static class TaskDetailBuilder
{
    /// <summary>Re-loads a task with all detail navigations and projects it (used after writes).</summary>
    public static async Task<TaskDetailDto> LoadAndBuildAsync(
        IAppDbContext db, IMemberDirectory members, Guid taskId, CancellationToken cancellationToken)
    {
        var task = await db.Tasks
            .AsNoTracking()
            .Include(t => t.Department)
            .Include(t => t.Assignees)
            .Include(t => t.Comments)
            .Include(t => t.Documents)
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken)
            ?? throw new NotFoundException("Task", taskId);

        return await BuildAsync(task, members, cancellationToken);
    }

    public static async Task<TaskDetailDto> BuildAsync(
        TaskItem task, IMemberDirectory members, CancellationToken cancellationToken)
    {
        var ids = new List<Guid>();
        if (task.CreatedById is { } creator)
        {
            ids.Add(creator);
        }

        ids.AddRange(task.Assignees.Select(a => a.MemberId));
        ids.AddRange(task.Comments.Select(c => c.AuthorId));
        ids.AddRange(task.Documents.Select(d => d.UploadedById));

        var map = await TaskMemberLookup.LoadAsync(members, ids, cancellationToken);

        var assignees = task.Assignees
            .Select(a => map.Find(a.MemberId))
            .OfType<MemberSummaryDto>()
            .ToList();

        var documents = task.Documents
            .OrderBy(d => d.UploadedAtUtc)
            .Select(d => new TaskDocumentDto(
                d.Id, d.OriginalFileName, d.ContentType, d.SizeBytes, map.Find(d.UploadedById), d.UploadedAtUtc))
            .ToList();

        var comments = task.Comments
            .OrderBy(c => c.CreatedAtUtc)
            .Select(c => new TaskCommentDto(c.Id, map.Find(c.AuthorId), c.Text, c.CreatedAtUtc))
            .ToList();

        return new TaskDetailDto(
            task.Id,
            task.Title,
            task.Description,
            task.Priority,
            task.Status,
            task.DueAtUtc,
            task.Scope,
            task.Department?.Code,
            task.CreatedById is { } createdBy ? map.Find(createdBy) : null,
            task.CreatedAtUtc,
            assignees,
            documents,
            comments);
    }
}
