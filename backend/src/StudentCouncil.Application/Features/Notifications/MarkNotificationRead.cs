using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;

namespace StudentCouncil.Application.Features.Notifications;

/// <summary>
/// Marks one notification read. A notification that isn't the caller's surfaces as <c>404</c> (not
/// <c>403</c>), so the endpoint never confirms the existence of another user's notification (plan 4.4).
/// </summary>
public sealed record MarkNotificationReadCommand(Guid Id) : IRequest;

public sealed class MarkNotificationReadHandler : IRequestHandler<MarkNotificationReadCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public MarkNotificationReadHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.Id ?? throw new UnauthorizedException();

        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.Id && n.RecipientId == userId, cancellationToken)
            ?? throw new NotFoundException("Notification", request.Id);

        if (notification.IsRead)
        {
            return;
        }

        notification.IsRead = true;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
