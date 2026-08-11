using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Common.Notifications;
using StudentCouncil.Application.Common.Security;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Application.Features.Tasks.Comments;

public sealed record AddTaskCommentRequest(string Text);

public sealed record AddTaskCommentCommand(Guid TaskId, string Text) : IRequest<TaskCommentDto>;

public sealed class AddTaskCommentValidator : AbstractValidator<AddTaskCommentCommand>
{
    public AddTaskCommentValidator() => RuleFor(x => x.Text).NotEmpty().Length(1, 2000);
}

public sealed class AddTaskCommentHandler : IRequestHandler<AddTaskCommentCommand, TaskCommentDto>
{
    private readonly IAppDbContext _db;
    private readonly IMemberDirectory _members;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationDispatcher _dispatcher;

    public AddTaskCommentHandler(
        IAppDbContext db, IMemberDirectory members, ICurrentUser currentUser, INotificationDispatcher dispatcher)
    {
        _db = db;
        _members = members;
        _currentUser = currentUser;
        _dispatcher = dispatcher;
    }

    public async Task<TaskCommentDto> Handle(AddTaskCommentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.Id ?? throw new UnauthorizedException();

        var task = await _db.Tasks
            .AsNoTracking()
            .Include(t => t.Assignees)
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);

        TaskAccess.EnsureCanView(task, _currentUser);

        var comment = new TaskComment
        {
            Id = Guid.NewGuid(),
            TaskItemId = request.TaskId,
            AuthorId = userId,
            Text = request.Text.Trim()
        };

        _db.TaskComments.Add(comment);
        await _db.SaveChangesAsync(cancellationToken);

        var authors = await TaskMemberLookup.LoadAsync(_members, [userId], cancellationToken);
        var author = authors.Find(userId);

        // Notify every other participant (assignees ∪ creator) — but not the comment's author.
        var recipients = await NotificationRecipients.TaskParticipantsAsync(_db, request.TaskId, cancellationToken, exclude: userId);
        if (recipients.Count > 0)
        {
            var content = NotificationTemplates.TaskComment(author?.FullName ?? "Член", task.Title);
            await _dispatcher.DispatchAsync(
                NotificationType.TaskComment, recipients, content.Title, content.Body,
                NotificationPayload.ForTask(request.TaskId), cancellationToken);
        }

        return new TaskCommentDto(comment.Id, author, comment.Text, comment.CreatedAtUtc);
    }
}
