using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Models;
using StudentCouncil.Application.Common.Security;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Application.Features.Tasks;

public sealed record GetTasksQuery(
    string? Scope = null,
    string? Department = null,
    Guid? Assignee = null,
    string? Priority = null,
    string? Status = null,
    DateTime? From = null,
    DateTime? To = null,
    bool? Overdue = null,
    string? Sort = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<TaskListItemDto>>;

public sealed class GetTasksHandler : IRequestHandler<GetTasksQuery, PagedResult<TaskListItemDto>>
{
    private const int MaxPageSize = 100;

    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;

    public GetTasksHandler(IAppDbContext db, ICurrentUser currentUser, IDateTime clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<PagedResult<TaskListItemDto>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize < 1 ? 20 : request.PageSize, 1, MaxPageSize);
        var now = _clock.UtcNow;

        var query = TaskAccess.Visible(_db.Tasks.AsNoTracking(), _currentUser);
        query = await ApplyFiltersAsync(query, request, now, cancellationToken);
        query = ApplySort(query, request.Sort);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(TaskMappings.ToListItem(now))
            .ToListAsync(cancellationToken);

        return new PagedResult<TaskListItemDto>(items, page, pageSize, totalCount);
    }

    private async Task<IQueryable<TaskItem>> ApplyFiltersAsync(
        IQueryable<TaskItem> query, GetTasksQuery request, DateTime now, CancellationToken cancellationToken)
    {
        if (Enum.TryParse<TaskScope>(request.Scope, ignoreCase: true, out var scope))
        {
            query = query.Where(t => t.Scope == scope);
        }

        if (Enum.TryParse<DepartmentCode>(request.Department, ignoreCase: true, out var code))
        {
            var departmentId = await _db.Departments
                .Where(d => d.Code == code)
                .Select(d => (Guid?)d.Id)
                .FirstOrDefaultAsync(cancellationToken);
            query = query.Where(t => t.DepartmentId == departmentId);
        }

        if (request.Assignee is { } assigneeId)
        {
            query = query.Where(t => t.Assignees.Any(a => a.MemberId == assigneeId));
        }

        if (Enum.TryParse<TaskPriority>(request.Priority, ignoreCase: true, out var priority))
        {
            query = query.Where(t => t.Priority == priority);
        }

        if (Enum.TryParse<TaskStatus>(request.Status, ignoreCase: true, out var status))
        {
            query = query.Where(t => t.Status == status);
        }

        if (request.From is { } from)
        {
            query = query.Where(t => t.DueAtUtc >= from);
        }

        if (request.To is { } to)
        {
            query = query.Where(t => t.DueAtUtc <= to);
        }

        if (request.Overdue == true)
        {
            query = query.Where(t => t.DueAtUtc != null
                && t.DueAtUtc < now
                && t.Status != TaskStatus.Completed
                && t.Status != TaskStatus.Cancelled);
        }

        return query;
    }

    private static IQueryable<TaskItem> ApplySort(IQueryable<TaskItem> query, string? sort)
    {
        var descending = sort?.StartsWith('-') == true;
        var field = sort?.TrimStart('-').ToLowerInvariant();

        return field switch
        {
            "dueat" => descending ? query.OrderByDescending(t => t.DueAtUtc) : query.OrderBy(t => t.DueAtUtc),
            "priority" => descending ? query.OrderByDescending(t => t.Priority) : query.OrderBy(t => t.Priority),
            "status" => descending ? query.OrderByDescending(t => t.Status) : query.OrderBy(t => t.Status),
            "title" => descending ? query.OrderByDescending(t => t.Title) : query.OrderBy(t => t.Title),
            _ => descending
                ? query.OrderBy(t => t.CreatedAtUtc)
                : query.OrderByDescending(t => t.CreatedAtUtc)
        };
    }
}
