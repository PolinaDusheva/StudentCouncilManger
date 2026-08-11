using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Options;
using StudentCouncil.Application.Features.Duties;
using StudentCouncil.Domain.Entities;
using StudentCouncil.Domain.Enums;
using StudentCouncil.Infrastructure.Identity;
using StudentCouncil.Infrastructure.Persistence;

namespace StudentCouncil.UnitTests.Duties;

public class GetDutySummaryHandlerTests
{
    private const int Year = 2026;
    private const int Month = 6;
    private const int Required = 120;

    private static readonly Guid MetExactly = Guid.NewGuid();
    private static readonly Guid UnderNorm = Guid.NewGuid();
    private static readonly Guid NoRecords = Guid.NewGuid();
    private static readonly Guid Inactive = Guid.NewGuid();

    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"duty-summary-{Guid.NewGuid()}").Options);

    private static ApplicationUser Member(Guid id, MemberStatus status) => new()
    {
        Id = id,
        Email = $"{id:N}@ue-varna.bg",
        UserName = $"{id:N}@ue-varna.bg",
        FullName = "Member",
        Role = SystemRole.Member,
        Status = status
    };

    private static DutyRecord Duty(Guid memberId, int minutes, int year = Year, int month = Month) => new()
    {
        Id = Guid.NewGuid(),
        MemberId = memberId,
        StartUtc = new DateTime(year, month, 1, 9, 0, 0, DateTimeKind.Utc),
        EndUtc = new DateTime(year, month, 1, 9, 0, 0, DateTimeKind.Utc).AddMinutes(minutes),
        DurationMinutes = minutes,
        PeriodYear = year,
        PeriodMonth = month,
        RecordedById = Guid.NewGuid()
    };

    private static GetDutySummaryHandler BuildHandler(AppDbContext db)
    {
        var clock = Substitute.For<IDateTime>();
        clock.UtcNow.Returns(new DateTime(Year, Month, 15, 0, 0, 0, DateTimeKind.Utc));
        var options = Options.Create(new DutyPolicyOptions { RequiredMinutesPerMonth = Required });
        return new GetDutySummaryHandler(db, new MemberDirectory(db), clock, options);
    }

    [Fact]
    public async Task Summary_covers_all_active_members_and_flags_the_norm()
    {
        await using var db = NewDb();
        db.Users.AddRange(
            Member(MetExactly, MemberStatus.Active),
            Member(UnderNorm, MemberStatus.Active),
            Member(NoRecords, MemberStatus.Active),
            Member(Inactive, MemberStatus.Inactive));

        db.DutyRecords.AddRange(
            // Exactly the norm, split across two shifts.
            Duty(MetExactly, 90),
            Duty(MetExactly, 30),
            // Below the norm.
            Duty(UnderNorm, 60),
            // A previous month's record must not count towards June.
            Duty(MetExactly, 500, month: 5),
            // The inactive member has plenty of minutes but should be excluded entirely.
            Duty(Inactive, 999));
        await db.SaveChangesAsync();

        var result = await BuildHandler(db).Handle(new GetDutySummaryQuery(Year, Month), default);

        // Inactive member excluded; the three active members all present.
        result.Should().HaveCount(3);
        result.Select(r => r.Member.Id).Should().BeEquivalentTo([MetExactly, UnderNorm, NoRecords]);

        var met = result.Single(r => r.Member.Id == MetExactly);
        met.TotalMinutes.Should().Be(120);
        met.RequiredMinutes.Should().Be(Required);
        met.MetNorm.Should().BeTrue("exactly 120 minutes meets the norm");

        result.Single(r => r.Member.Id == UnderNorm).MetNorm.Should().BeFalse();

        var noRecords = result.Single(r => r.Member.Id == NoRecords);
        noRecords.TotalMinutes.Should().Be(0);
        noRecords.MetNorm.Should().BeFalse("a member with no records is under norm");

        // Under-norm members are listed first.
        result.Take(2).Should().OnlyContain(r => !r.MetNorm);
    }
}
