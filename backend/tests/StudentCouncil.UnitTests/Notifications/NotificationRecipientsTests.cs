using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Common.Notifications;
using StudentCouncil.Domain.Entities;
using StudentCouncil.Domain.Enums;
using StudentCouncil.Infrastructure.Identity;
using StudentCouncil.Infrastructure.Persistence;

namespace StudentCouncil.UnitTests.Notifications;

public class NotificationRecipientsTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"notif-recipients-{Guid.NewGuid()}").Options);

    private static ApplicationUser Member(Guid id, MemberStatus status = MemberStatus.Active) => new()
    {
        Id = id,
        Email = $"{id:N}@ue-varna.bg",
        UserName = $"{id:N}@ue-varna.bg",
        FullName = "Member",
        Role = SystemRole.Member,
        Status = status
    };

    [Fact]
    public async Task Task_participants_are_assignees_plus_creator()
    {
        var creator = Guid.NewGuid();
        var assigneeA = Guid.NewGuid();
        var assigneeB = Guid.NewGuid();

        await using var db = NewDb();
        var task = new TaskItem { Id = Guid.NewGuid(), Title = "T", CreatedById = creator };
        task.Assignees.Add(new TaskAssignee { TaskItemId = task.Id, MemberId = assigneeA });
        task.Assignees.Add(new TaskAssignee { TaskItemId = task.Id, MemberId = assigneeB });
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        var recipients = await NotificationRecipients.TaskParticipantsAsync(db, task.Id, default);

        recipients.Should().BeEquivalentTo([creator, assigneeA, assigneeB]);
    }

    [Fact]
    public async Task Task_participants_exclude_the_named_member()
    {
        var creator = Guid.NewGuid();
        var assignee = Guid.NewGuid();

        await using var db = NewDb();
        var task = new TaskItem { Id = Guid.NewGuid(), Title = "T", CreatedById = creator };
        task.Assignees.Add(new TaskAssignee { TaskItemId = task.Id, MemberId = assignee });
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        // A comment by the assignee should not notify themselves.
        var recipients = await NotificationRecipients.TaskParticipantsAsync(db, task.Id, default, exclude: assignee);

        recipients.Should().BeEquivalentTo([creator]);
    }

    [Fact]
    public async Task Event_recipients_fall_back_to_all_active_members_when_no_participants()
    {
        var active = Guid.NewGuid();
        var alsoActive = Guid.NewGuid();
        var inactive = Guid.NewGuid();

        await using var db = NewDb();
        db.Users.AddRange(Member(active), Member(alsoActive), Member(inactive, MemberStatus.Inactive));
        await db.SaveChangesAsync();

        var recipients = await NotificationRecipients.EventRecipientsAsync(new MemberDirectory(db), [], default);

        recipients.Should().BeEquivalentTo([active, alsoActive]);
    }

    [Fact]
    public async Task Event_recipients_use_the_explicit_participants_when_present()
    {
        var participant = Guid.NewGuid();

        await using var db = NewDb();
        db.Users.AddRange(Member(participant), Member(Guid.NewGuid()));
        await db.SaveChangesAsync();

        var recipients = await NotificationRecipients.EventRecipientsAsync(new MemberDirectory(db), [participant], default);

        recipients.Should().BeEquivalentTo([participant]);
    }

    [Fact]
    public async Task Under_norm_returns_only_active_members_below_the_norm()
    {
        var met = Guid.NewGuid();
        var under = Guid.NewGuid();
        var noRecords = Guid.NewGuid();
        var inactive = Guid.NewGuid();

        await using var db = NewDb();
        db.Users.AddRange(
            Member(met), Member(under), Member(noRecords), Member(inactive, MemberStatus.Inactive));
        db.DutyRecords.AddRange(
            Duty(met, 120),
            Duty(under, 60),
            Duty(inactive, 999));
        await db.SaveChangesAsync();

        var recipients = await NotificationRecipients.UnderDutyNormAsync(
            db, new MemberDirectory(db), 2026, 6, requiredMinutes: 120, default);

        // `met` reached the norm and `inactive` is excluded entirely; `under` and `noRecords` remain.
        recipients.Should().BeEquivalentTo([under, noRecords]);
    }

    private static DutyRecord Duty(Guid memberId, int minutes) => new()
    {
        Id = Guid.NewGuid(),
        MemberId = memberId,
        StartUtc = new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc),
        EndUtc = new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc).AddMinutes(minutes),
        DurationMinutes = minutes,
        PeriodYear = 2026,
        PeriodMonth = 6,
        RecordedById = Guid.NewGuid()
    };
}
