using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Common.Notifications;
using StudentCouncil.Application.Common.Security;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Application.Features.Events;

/// <summary>Body of <c>PUT /events/{id}</c>; the route supplies the id.</summary>
public sealed record UpdateEventRequest(
    string Title,
    string? Description,
    DateTime StartUtc,
    DateTime EndUtc,
    string? Location,
    EventType Type,
    Guid? DepartmentId,
    RecurrenceType Recurrence,
    IReadOnlyList<Guid>? ParticipantIds);

public sealed record UpdateEventCommand(
    Guid Id,
    string Title,
    string? Description,
    DateTime StartUtc,
    DateTime EndUtc,
    string? Location,
    EventType Type,
    Guid? DepartmentId,
    RecurrenceType Recurrence,
    IReadOnlyList<Guid>? ParticipantIds) : IRequest<EventMutationResult>, IEventInput
{
    public static UpdateEventCommand From(Guid id, UpdateEventRequest request) =>
        new(id, request.Title, request.Description, request.StartUtc, request.EndUtc, request.Location,
            request.Type, request.DepartmentId, request.Recurrence, request.ParticipantIds);
}

public sealed class UpdateEventValidator : AbstractValidator<UpdateEventCommand>
{
    public UpdateEventValidator(IAppDbContext db, IMemberDirectory members)
    {
        EventRules.Apply(this, db, members);
    }
}

public sealed class UpdateEventHandler : IRequestHandler<UpdateEventCommand, EventMutationResult>
{
    private readonly IAppDbContext _db;
    private readonly IMemberDirectory _members;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationDispatcher _dispatcher;

    public UpdateEventHandler(
        IAppDbContext db, IMemberDirectory members, ICurrentUser currentUser, INotificationDispatcher dispatcher)
    {
        _db = db;
        _members = members;
        _currentUser = currentUser;
        _dispatcher = dispatcher;
    }

    public async Task<EventMutationResult> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        var calendarEvent = await _db.CalendarEvents
            .Include(e => e.Participants)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Event", request.Id);

        EventAccess.EnsureCanEdit(calendarEvent, _currentUser);

        calendarEvent.Title = request.Title.Trim();
        calendarEvent.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        // A moved start time must re-arm the 24h/1h reminders (decision #7).
        if (calendarEvent.StartUtc != request.StartUtc)
        {
            calendarEvent.Reminder24hSentAtUtc = null;
            calendarEvent.Reminder1hSentAtUtc = null;
        }

        calendarEvent.StartUtc = request.StartUtc;
        calendarEvent.EndUtc = request.EndUtc;
        calendarEvent.Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim();
        calendarEvent.Type = request.Type;
        calendarEvent.DepartmentId = request.DepartmentId;
        calendarEvent.Recurrence = request.Recurrence;

        ReplaceParticipants(calendarEvent, request.ParticipantIds ?? []);

        await _db.SaveChangesAsync(cancellationToken);

        var participantIds = (request.ParticipantIds ?? []).Distinct().ToList();
        var recipients = await NotificationRecipients.EventRecipientsAsync(_members, participantIds, cancellationToken);
        var content = NotificationTemplates.EventChanged(calendarEvent.Title);
        await _dispatcher.DispatchAsync(
            NotificationType.EventChanged, recipients, content.Title, content.Body,
            NotificationPayload.ForEvent(calendarEvent.Id), cancellationToken);

        var conflicts = await EventConflicts.FindAsync(
            _db, calendarEvent.Id, calendarEvent.StartUtc, calendarEvent.EndUtc, cancellationToken);
        var detail = await EventDetailBuilder.LoadAndBuildAsync(_db, _members, calendarEvent.Id, cancellationToken);

        return new EventMutationResult(detail, conflicts);
    }

    private void ReplaceParticipants(CalendarEvent calendarEvent, IReadOnlyList<Guid> participantIds)
    {
        var desired = participantIds.Distinct().ToHashSet();
        var current = calendarEvent.Participants.Select(p => p.MemberId).ToHashSet();

        var toRemove = calendarEvent.Participants.Where(p => !desired.Contains(p.MemberId)).ToList();
        _db.EventParticipants.RemoveRange(toRemove);

        foreach (var memberId in desired.Where(id => !current.Contains(id)))
        {
            _db.EventParticipants.Add(new EventParticipant { CalendarEventId = calendarEvent.Id, MemberId = memberId });
        }
    }
}
