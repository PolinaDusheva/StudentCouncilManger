using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Common.Notifications;
using StudentCouncil.Application.Features.Tasks;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Application.Features.Events;

public sealed record CreateEventCommand(
    string Title,
    string? Description,
    DateTime StartUtc,
    DateTime EndUtc,
    string? Location,
    EventType Type,
    Guid? DepartmentId,
    RecurrenceType Recurrence,
    IReadOnlyList<Guid>? ParticipantIds) : IRequest<EventMutationResult>, IEventInput;

public sealed class CreateEventValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventValidator(IAppDbContext db, IMemberDirectory members)
    {
        EventRules.Apply(this, db, members);
    }
}

public sealed class CreateEventHandler : IRequestHandler<CreateEventCommand, EventMutationResult>
{
    private readonly IAppDbContext _db;
    private readonly IMemberDirectory _members;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationDispatcher _dispatcher;

    public CreateEventHandler(
        IAppDbContext db, IMemberDirectory members, ICurrentUser currentUser, INotificationDispatcher dispatcher)
    {
        _db = db;
        _members = members;
        _currentUser = currentUser;
        _dispatcher = dispatcher;
    }

    public async Task<EventMutationResult> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var organizerId = _currentUser.Id ?? throw new UnauthorizedException();

        var calendarEvent = new CalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            StartUtc = request.StartUtc,
            EndUtc = request.EndUtc,
            Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim(),
            Type = request.Type,
            DepartmentId = request.DepartmentId,
            OrganizerId = organizerId,
            Recurrence = request.Recurrence
        };

        foreach (var memberId in (request.ParticipantIds ?? []).Distinct())
        {
            calendarEvent.Participants.Add(new EventParticipant { CalendarEventId = calendarEvent.Id, MemberId = memberId });
        }

        _db.CalendarEvents.Add(calendarEvent);
        await _db.SaveChangesAsync(cancellationToken);

        var participantIds = calendarEvent.Participants.Select(p => p.MemberId).ToList();
        var recipients = await NotificationRecipients.EventRecipientsAsync(_members, participantIds, cancellationToken);
        var content = NotificationTemplates.EventCreated(calendarEvent.Title);
        await _dispatcher.DispatchAsync(
            NotificationType.EventCreated, recipients, content.Title, content.Body,
            NotificationPayload.ForEvent(calendarEvent.Id), cancellationToken);

        var conflicts = await EventConflicts.FindAsync(
            _db, calendarEvent.Id, calendarEvent.StartUtc, calendarEvent.EndUtc, cancellationToken);
        var detail = await EventDetailBuilder.LoadAndBuildAsync(_db, _members, calendarEvent.Id, cancellationToken);

        return new EventMutationResult(detail, conflicts);
    }
}

/// <summary>Validation rules shared by create and update (spec 11, plan 4.2.1).</summary>
internal static class EventRules
{
    public static void Apply<T>(AbstractValidator<T> validator, IAppDbContext db, IMemberDirectory members)
        where T : IEventInput
    {
        validator.RuleFor(x => x.Title).NotEmpty().Length(3, 150);
        validator.RuleFor(x => x.Description).MaximumLength(4000);
        validator.RuleFor(x => x.Location).MaximumLength(300);
        validator.RuleFor(x => x.Type).IsInEnum();
        validator.RuleFor(x => x.Recurrence).IsInEnum();

        validator.RuleFor(x => x.EndUtc)
            .GreaterThan(x => x.StartUtc)
            .WithMessage("The end time must be after the start time.");

        validator.RuleFor(x => x.DepartmentId!)
            .MustAsync(async (id, ct) => await db.Departments.AnyAsync(d => d.Id == id, ct))
            .When(x => x.DepartmentId.HasValue)
            .WithMessage("The specified department does not exist.");

        validator.RuleFor(x => x.ParticipantIds!)
            .MustAsync((ids, ct) => CreateTaskValidator.AllActiveMembersAsync(members, ids, ct))
            .When(x => x.ParticipantIds is { Count: > 0 })
            .WithMessage("Every participant must be an existing, active member.");
    }
}

/// <summary>Common shape of the create/update event inputs so the rules can be written once.</summary>
internal interface IEventInput
{
    string Title { get; }
    string? Description { get; }
    string? Location { get; }
    DateTime StartUtc { get; }
    DateTime EndUtc { get; }
    EventType Type { get; }
    RecurrenceType Recurrence { get; }
    Guid? DepartmentId { get; }
    IReadOnlyList<Guid>? ParticipantIds { get; }
}
