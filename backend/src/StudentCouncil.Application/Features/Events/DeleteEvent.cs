using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Common.Notifications;
using StudentCouncil.Application.Common.Security;

namespace StudentCouncil.Application.Features.Events;

/// <summary>
/// Physically deletes an event (<see cref="Domain.Entities.CalendarEvent"/> is not soft-deletable, so a
/// plain remove is a true delete; participants fall away via the cascade). Delete is leadership-only —
/// a secretary who organised the event may edit but not delete it (decision #2).
/// </summary>
public sealed record DeleteEventCommand(Guid Id) : IRequest;

public sealed class DeleteEventHandler : IRequestHandler<DeleteEventCommand>
{
    private readonly IAppDbContext _db;
    private readonly IMemberDirectory _members;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationDispatcher _dispatcher;

    public DeleteEventHandler(
        IAppDbContext db, IMemberDirectory members, ICurrentUser currentUser, INotificationDispatcher dispatcher)
    {
        _db = db;
        _members = members;
        _currentUser = currentUser;
        _dispatcher = dispatcher;
    }

    public async Task Handle(DeleteEventCommand request, CancellationToken cancellationToken)
    {
        var calendarEvent = await _db.CalendarEvents
            .Include(e => e.Participants)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Event", request.Id);

        EventAccess.EnsureCanDelete(calendarEvent, _currentUser);

        // Resolve recipients before the delete — the participant rows go away with the event.
        var participantIds = calendarEvent.Participants.Select(p => p.MemberId).ToList();
        var title = calendarEvent.Title;
        var recipients = await NotificationRecipients.EventRecipientsAsync(_members, participantIds, cancellationToken);

        _db.CalendarEvents.Remove(calendarEvent);
        await _db.SaveChangesAsync(cancellationToken);

        var content = NotificationTemplates.EventCancelled(title);
        await _dispatcher.DispatchAsync(
            NotificationType.EventChanged, recipients, content.Title, content.Body,
            NotificationPayload.ForEvent(request.Id), cancellationToken);
    }
}
