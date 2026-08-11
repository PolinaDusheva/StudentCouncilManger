using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Notifications;
using StudentCouncil.Application.Common.Options;

namespace StudentCouncil.Application.Features.Duties;

/// <summary>
/// Reminds about duty: a specific member, or everyone under norm for the current month. The recipients are
/// resolved here and handed to the dispatcher (decision #10); the response reports how many were notified.
/// </summary>
public sealed record RemindDutiesCommand(Guid? MemberId = null) : IRequest<RemindResult>;

public sealed class RemindDutiesHandler : IRequestHandler<RemindDutiesCommand, RemindResult>
{
    private readonly IAppDbContext _db;
    private readonly IMemberDirectory _members;
    private readonly IDateTime _clock;
    private readonly IOptions<DutyPolicyOptions> _dutyPolicy;
    private readonly INotificationDispatcher _dispatcher;

    public RemindDutiesHandler(
        IAppDbContext db,
        IMemberDirectory members,
        IDateTime clock,
        IOptions<DutyPolicyOptions> dutyPolicy,
        INotificationDispatcher dispatcher)
    {
        _db = db;
        _members = members;
        _clock = clock;
        _dutyPolicy = dutyPolicy;
        _dispatcher = dispatcher;
    }

    public async Task<RemindResult> Handle(RemindDutiesCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var recipients = await ResolveRecipientsAsync(request.MemberId, now, cancellationToken);

        if (recipients.Count > 0)
        {
            var content = NotificationTemplates.DutyReminder(now.Year, now.Month);
            await _dispatcher.DispatchAsync(
                NotificationType.DutyReminder, recipients, content.Title, content.Body,
                NotificationPayload.ForDuty(), cancellationToken);
        }

        return new RemindResult(recipients.Count);
    }

    private async Task<IReadOnlyList<Guid>> ResolveRecipientsAsync(
        Guid? memberId, DateTime now, CancellationToken cancellationToken)
    {
        if (memberId is { } id)
        {
            // A specific member is a valid recipient only if they are an active member.
            var isActive = await _members.Members
                .AnyAsync(m => m.Id == id && m.Status == MemberStatus.Active, cancellationToken);
            return isActive ? [id] : [];
        }

        var required = _dutyPolicy.Value.RequiredMinutesPerMonth;
        return await NotificationRecipients.UnderDutyNormAsync(
            _db, _members, now.Year, now.Month, required, cancellationToken);
    }
}
