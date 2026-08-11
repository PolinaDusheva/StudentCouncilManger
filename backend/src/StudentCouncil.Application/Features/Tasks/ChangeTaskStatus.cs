using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Common.Notifications;
using StudentCouncil.Application.Common.Security;

namespace StudentCouncil.Application.Features.Tasks;

public sealed record ChangeTaskStatusRequest(TaskStatus Status);

public sealed record ChangeTaskStatusCommand(Guid Id, TaskStatus Status) : IRequest<TaskDetailDto>;

public sealed class ChangeTaskStatusValidator : AbstractValidator<ChangeTaskStatusCommand>
{
    public ChangeTaskStatusValidator() => RuleFor(x => x.Status).IsInEnum();
}

public sealed class ChangeTaskStatusHandler : IRequestHandler<ChangeTaskStatusCommand, TaskDetailDto>
{
    private readonly IAppDbContext _db;
    private readonly IMemberDirectory _members;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationDispatcher _dispatcher;

    public ChangeTaskStatusHandler(
        IAppDbContext db, IMemberDirectory members, ICurrentUser currentUser, INotificationDispatcher dispatcher)
    {
        _db = db;
        _members = members;
        _currentUser = currentUser;
        _dispatcher = dispatcher;
    }

    public async Task<TaskDetailDto> Handle(ChangeTaskStatusCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.Id ?? throw new UnauthorizedException();

        var task = await _db.Tasks
            .Include(t => t.Assignees)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Task", request.Id);

        // Invisible -> 404 before any role reasoning.
        TaskAccess.EnsureCanView(task, _currentUser);

        var actorIsLeadership = TaskAccess.CanEdit(task, _currentUser);
        var isAssignee = TaskAccess.IsAssignee(task, userId);
        if (!isAssignee && !actorIsLeadership)
        {
            throw new ForbiddenException("You do not have permission to change this task's status.");
        }

        TaskStatusTransitions.EnsureAllowed(task.Status, request.Status, actorIsLeadership);

        task.Status = request.Status;
        await _db.SaveChangesAsync(cancellationToken);

        // Notify the task's creator of the status change (skip if the actor is the creator).
        if (task.CreatedById is { } creatorId && creatorId != userId)
        {
            var content = NotificationTemplates.TaskStatusChanged(task.Title, request.Status);
            await _dispatcher.DispatchAsync(
                NotificationType.TaskStatusChanged, [creatorId], content.Title, content.Body,
                NotificationPayload.ForTask(task.Id), cancellationToken);
        }

        return await TaskDetailBuilder.LoadAndBuildAsync(_db, _members, task.Id, cancellationToken);
    }
}
