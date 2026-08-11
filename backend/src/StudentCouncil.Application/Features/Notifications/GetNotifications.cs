using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Common.Models;
using StudentCouncil.Application.Common.Notifications;

namespace StudentCouncil.Application.Features.Notifications;

/// <summary>The caller's own in-app notifications, newest first (spec 7.9). Never returns another user's.</summary>
public sealed record GetNotificationsQuery(
    bool UnreadOnly = false,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<NotificationDto>>;

public sealed class GetNotificationsHandler : IRequestHandler<GetNotificationsQuery, PagedResult<NotificationDto>>
{
    private const int MaxPageSize = 100;

    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetNotificationsHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.Id ?? throw new UnauthorizedException();
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize < 1 ? 20 : request.PageSize, 1, MaxPageSize);

        var query = _db.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientId == userId);

        if (request.UnreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        query = query.OrderByDescending(n => n.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var rows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(n => new NotificationDto(
                n.Id, n.Type, n.Title, n.Body, NotificationPayload.FromJson(n.Payload), n.IsRead, n.CreatedAtUtc))
            .ToList();

        return new PagedResult<NotificationDto>(items, page, pageSize, totalCount);
    }
}
