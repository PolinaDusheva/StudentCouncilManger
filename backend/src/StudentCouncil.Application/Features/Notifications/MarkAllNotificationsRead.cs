using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;

namespace StudentCouncil.Application.Features.Notifications;

/// <summary>Marks every unread notification of the caller as read in a single set-based update (plan 4.4).</summary>
public sealed record MarkAllNotificationsReadCommand : IRequest;

public sealed class MarkAllNotificationsReadHandler : IRequestHandler<MarkAllNotificationsReadCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public MarkAllNotificationsReadHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.Id ?? throw new UnauthorizedException();

        await _db.Notifications
            .Where(n => n.RecipientId == userId && !n.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true), cancellationToken);
    }
}
