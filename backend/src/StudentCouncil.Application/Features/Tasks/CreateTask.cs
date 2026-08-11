using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Common.Notifications;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Application.Features.Tasks;

public sealed record CreateTaskCommand(
    string Title,
    string? Description,
    TaskPriority Priority,
    TaskScope Scope,
    Guid? DepartmentId,
    DateTime? DueAtUtc,
    IReadOnlyList<Guid> AssigneeIds) : IRequest<TaskDetailDto>;

public sealed class CreateTaskValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskValidator(IAppDbContext db, IMemberDirectory members, IDateTime clock)
    {
        RuleFor(x => x.Title).NotEmpty().Length(3, 150);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.Scope).IsInEnum();

        RuleFor(x => x.DepartmentId)
            .NotNull().When(x => x.Scope == TaskScope.Departmental)
            .WithMessage("Departmental tasks require a department.");

        RuleFor(x => x.DepartmentId)
            .Null().When(x => x.Scope == TaskScope.Organizational)
            .WithMessage("Organisational tasks must not specify a department.");

        RuleFor(x => x.DepartmentId!)
            .MustAsync(async (id, ct) => await db.Departments.AnyAsync(d => d.Id == id, ct))
            .When(x => x.DepartmentId.HasValue)
            .WithMessage("The specified department does not exist.");

        RuleFor(x => x.AssigneeIds)
            .NotEmpty().WithMessage("At least one assignee is required.");

        RuleFor(x => x.AssigneeIds)
            .MustAsync((ids, ct) => AllActiveMembersAsync(members, ids, ct))
            .When(x => x.AssigneeIds is { Count: > 0 })
            .WithMessage("Every assignee must be an existing, active member.");

        RuleFor(x => x.DueAtUtc!)
            .Must(due => due > clock.UtcNow)
            .When(x => x.DueAtUtc.HasValue)
            .WithMessage("The due date must be in the future.");
    }

    internal static async Task<bool> AllActiveMembersAsync(
        IMemberDirectory members, IReadOnlyList<Guid> ids, CancellationToken cancellationToken)
    {
        var distinct = ids.Distinct().ToList();
        var activeCount = await members.Members
            .Where(m => distinct.Contains(m.Id) && m.Status == MemberStatus.Active)
            .CountAsync(cancellationToken);
        return activeCount == distinct.Count;
    }
}

public sealed class CreateTaskHandler : IRequestHandler<CreateTaskCommand, TaskDetailDto>
{
    private readonly IAppDbContext _db;
    private readonly IMemberDirectory _members;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationDispatcher _dispatcher;

    public CreateTaskHandler(
        IAppDbContext db, IMemberDirectory members, ICurrentUser currentUser, INotificationDispatcher dispatcher)
    {
        _db = db;
        _members = members;
        _currentUser = currentUser;
        _dispatcher = dispatcher;
    }

    public async Task<TaskDetailDto> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        EnsureScopeAllowed(request);

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Priority = request.Priority,
            Status = TaskStatus.New,
            Scope = request.Scope,
            DepartmentId = request.DepartmentId,
            DueAtUtc = request.DueAtUtc
        };

        foreach (var memberId in request.AssigneeIds.Distinct())
        {
            task.Assignees.Add(new TaskAssignee { TaskItemId = task.Id, MemberId = memberId });
        }

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(cancellationToken);

        var assignees = task.Assignees.Select(a => a.MemberId).ToList();
        var content = NotificationTemplates.TaskAssigned(task.Title);
        await _dispatcher.DispatchAsync(
            NotificationType.TaskAssigned, assignees, content.Title, content.Body,
            NotificationPayload.ForTask(task.Id), cancellationToken);

        return await TaskDetailBuilder.LoadAndBuildAsync(_db, _members, task.Id, cancellationToken);
    }

    /// <summary>
    /// Decision #3: the controller's <c>CanCreateDeptTask</c> gate is the broad door; the precise rule
    /// lives here. Organisational tasks need Org leadership; Departmental tasks let Dept leadership act
    /// only within their own department, while Org leadership may target any department.
    /// </summary>
    private void EnsureScopeAllowed(CreateTaskCommand request)
    {
        var role = _currentUser.Role ?? throw new UnauthorizedException();
        var isOrgLeadership = role is SystemRole.OrgPresident or SystemRole.OrgVicePresident;

        if (request.Scope == TaskScope.Organizational && !isOrgLeadership)
        {
            throw new ForbiddenException("Only organisation leadership can create organisational tasks.");
        }

        if (request.Scope == TaskScope.Departmental && !isOrgLeadership)
        {
            var isDeptLeadership = role is SystemRole.DeptPresident or SystemRole.DeptVicePresident;
            if (!isDeptLeadership || request.DepartmentId != _currentUser.DepartmentId)
            {
                throw new ForbiddenException("You can only create tasks for your own department.");
            }
        }
    }
}
