using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Features.Duties;

namespace StudentCouncil.Application.Common.Notifications;

/// <summary>
/// Pure recipient resolvers shared by the synchronous seams and the background jobs (decision #13), so
/// the "who gets notified" rules live in one place and stay unit-testable.
/// </summary>
public static class NotificationRecipients
{
    /// <summary>
    /// Everyone involved in a task: its assignees plus the creator, optionally excluding one member
    /// (e.g. the comment author shouldn't be notified of their own comment).
    /// </summary>
    public static async Task<IReadOnlyList<Guid>> TaskParticipantsAsync(
        IAppDbContext db, Guid taskId, CancellationToken cancellationToken, Guid? exclude = null)
    {
        var task = await db.Tasks
            .AsNoTracking()
            .Where(t => t.Id == taskId)
            .Select(t => new { t.CreatedById, AssigneeIds = t.Assignees.Select(a => a.MemberId).ToList() })
            .FirstOrDefaultAsync(cancellationToken);

        if (task is null)
        {
            return [];
        }

        var recipients = new HashSet<Guid>(task.AssigneeIds);
        if (task.CreatedById is { } creatorId)
        {
            recipients.Add(creatorId);
        }

        if (exclude is { } excluded)
        {
            recipients.Remove(excluded);
        }

        return recipients.ToList();
    }

    /// <summary>
    /// Who should hear about an event: its explicit participants, or — when none are specified — every
    /// active member (an empty participant list means "visible to everyone", spec 4.3 / functional 8.2).
    /// </summary>
    public static async Task<IReadOnlyList<Guid>> EventRecipientsAsync(
        IMemberDirectory members, IReadOnlyCollection<Guid> participantIds, CancellationToken cancellationToken)
    {
        if (participantIds.Count > 0)
        {
            return participantIds.Distinct().ToList();
        }

        return await members.Members
            .Where(m => m.Status == MemberStatus.Active)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Active members below the monthly duty norm — the broadcast duty-reminder audience, shared by the
    /// <c>/duty-records/remind</c> endpoint and the monthly archive job.
    /// </summary>
    public static async Task<IReadOnlyList<Guid>> UnderDutyNormAsync(
        IAppDbContext db, IMemberDirectory members, int year, int month, int requiredMinutes,
        CancellationToken cancellationToken)
    {
        var activeMemberIds = await members.Members
            .Where(m => m.Status == MemberStatus.Active)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        var totals = await DutyTotals.ByMemberAsync(db, year, month, cancellationToken);

        return activeMemberIds
            .Where(memberId => totals.GetValueOrDefault(memberId, 0) < requiredMinutes)
            .ToList();
    }
}
