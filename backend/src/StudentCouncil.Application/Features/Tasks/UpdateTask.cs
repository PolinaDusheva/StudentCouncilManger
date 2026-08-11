using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Common.Notifications;
using StudentCouncil.Application.Common.Security;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Application.Features.Tasks;

/// <summary>Body of <c>PUT /tasks/{id}</c>. Scope and department are immutable, so they are omitted.</summary>
public sealed record UpdateTaskRequest(
    string Title,
    string? Description,
    TaskPriority Priority,
    DateTime? DueAtUtc,
    IReadOnlyList<Guid> AssigneeIds);

public sealed record UpdateTaskCommand(
    Guid Id,
    string Title,
    string? Description,
    TaskPriority Priority,
    DateTime? DueAtUtc,
    IReadOnlyList<Guid> AssigneeIds) : IRequest<TaskDetailDto>
{
    public static UpdateTaskCommand From(Guid id, UpdateTaskRequest request) =>
        new(id, request.Title, request.Description, request.Priority, request.DueAtUtc, request.AssigneeIds);
}

public sealed class UpdateTaskValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskValidator(IMemberDirectory members)
    {
        RuleFor(x => x.Title).NotEmpty().Length(3, 150);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Priority).IsInEnum();

        RuleFor(x => x.AssigneeIds)
            .NotEmpty().WithMessage("At least one assignee is required.");

        RuleFor(x => x.AssigneeIds)
            .MustAsync((ids, ct) => CreateTaskValidator.AllActiveMembersAsync(members, ids, ct))
            .When(x => x.AssigneeIds is { Count: > 0 })
            .WithMessage("Every assignee must be an existing, active member.");
    }
}

public sealed class UpdateTaskHandler : IRequestHandler<UpdateTaskCommand, TaskDetailDto>
{
    private readonly IAppDbContext _db;
    private readonly IMemberDirectory _members;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationDispatcher _dispatcher;

    public UpdateTaskHandler(
        IAppDbContext db, IMemberDirectory members, ICurrentUser currentUser, INotificationDispatcher dispatcher)
    {
        _db = db;
        _members = members;
        _currentUser = currentUser;
        _dispatcher = dispatcher;
    }

    public async Task<TaskDetailDto> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _db.Tasks
            .Include(t => t.Assignees)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Task", request.Id);

        TaskAccess.EnsureCanEdit(task, _currentUser);

        task.Title = request.Title.Trim();
        task.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        task.Priority = request.Priority;

        // A moved due date must re-arm the reminders (decision #7), so a rescheduled task notifies again.
        if (task.DueAtUtc != request.DueAtUtc)
        {
            task.DueSoonNotifiedAtUtc = null;
            task.OverdueNotifiedAtUtc = null;
        }

        task.DueAtUtc = request.DueAtUtc;

        var addedAssignees = ReplaceAssignees(task, request.AssigneeIds);

        await _db.SaveChangesAsync(cancellationToken);

        // Only the newly added assignees are notified — existing ones already know about the task.
        if (addedAssignees.Count > 0)
        {
            var content = NotificationTemplates.TaskAssigned(task.Title);
            await _dispatcher.DispatchAsync(
                NotificationType.TaskAssigned, addedAssignees, content.Title, content.Body,
                NotificationPayload.ForTask(task.Id), cancellationToken);
        }

        return await TaskDetailBuilder.LoadAndBuildAsync(_db, _members, task.Id, cancellationToken);
    }

    private List<Guid> ReplaceAssignees(TaskItem task, IReadOnlyList<Guid> assigneeIds)
    {
        var desired = assigneeIds.Distinct().ToHashSet();
        var current = task.Assignees.Select(a => a.MemberId).ToHashSet();

        var toRemove = task.Assignees.Where(a => !desired.Contains(a.MemberId)).ToList();
        _db.TaskAssignees.RemoveRange(toRemove);

        var added = desired.Where(id => !current.Contains(id)).ToList();
        foreach (var memberId in added)
        {
            _db.TaskAssignees.Add(new TaskAssignee { TaskItemId = task.Id, MemberId = memberId });
        }

        return added;
    }
}
