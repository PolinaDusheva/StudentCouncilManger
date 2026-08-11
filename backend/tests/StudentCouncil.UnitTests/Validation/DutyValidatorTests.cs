using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Features.Duties;
using StudentCouncil.Domain.Entities;
using StudentCouncil.Domain.Enums;
using StudentCouncil.Infrastructure.Identity;
using StudentCouncil.Infrastructure.Persistence;

namespace StudentCouncil.UnitTests.Validation;

public class DutyValidatorTests
{
    private static readonly Guid ActiveMember = Guid.NewGuid();
    private static readonly Guid InactiveMember = Guid.NewGuid();
    private static readonly DateTime Start = new(2026, 6, 10, 9, 0, 0, DateTimeKind.Utc);

    private static async Task<AppDbContext> BuildDbAsync()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"duties-{Guid.NewGuid()}")
            .Options);

        db.Users.Add(Member(ActiveMember, "duty.active@ue-varna.bg", MemberStatus.Active));
        db.Users.Add(Member(InactiveMember, "duty.inactive@ue-varna.bg", MemberStatus.Inactive));
        await db.SaveChangesAsync();
        return db;
    }

    private static ApplicationUser Member(Guid id, string email, MemberStatus status) => new()
    {
        Id = id,
        Email = email,
        UserName = email,
        FullName = "Member",
        Role = SystemRole.Member,
        Status = status
    };

    [Fact]
    public async Task End_before_start_is_rejected()
    {
        await using var db = await BuildDbAsync();
        var validator = new CreateDutyRecordValidator(new MemberDirectory(db));

        var result = await validator.ValidateAsync(
            new CreateDutyRecordCommand(ActiveMember, Start, Start.AddHours(-1), null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Sub_minute_duration_is_rejected()
    {
        await using var db = await BuildDbAsync();
        var validator = new CreateDutyRecordValidator(new MemberDirectory(db));

        var result = await validator.ValidateAsync(
            new CreateDutyRecordCommand(ActiveMember, Start, Start.AddSeconds(30), null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Inactive_member_is_rejected()
    {
        await using var db = await BuildDbAsync();
        var validator = new CreateDutyRecordValidator(new MemberDirectory(db));

        var result = await validator.ValidateAsync(
            new CreateDutyRecordCommand(InactiveMember, Start, Start.AddHours(2), null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task A_well_formed_duty_passes()
    {
        await using var db = await BuildDbAsync();
        var validator = new CreateDutyRecordValidator(new MemberDirectory(db));

        var result = await validator.ValidateAsync(
            new CreateDutyRecordCommand(ActiveMember, Start, Start.AddHours(2), "Front desk"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Update_validator_enforces_end_after_start()
    {
        var validator = new UpdateDutyRecordValidator();

        var invalid = await validator.ValidateAsync(new UpdateDutyRecordCommand(Guid.NewGuid(), Start, Start, null));
        var valid = await validator.ValidateAsync(
            new UpdateDutyRecordCommand(Guid.NewGuid(), Start, Start.AddHours(1), null));

        invalid.IsValid.Should().BeFalse();
        valid.IsValid.Should().BeTrue();
    }
}
