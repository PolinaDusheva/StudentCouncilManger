using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Features.Events;
using StudentCouncil.Domain.Entities;
using StudentCouncil.Domain.Enums;
using StudentCouncil.Infrastructure.Identity;
using StudentCouncil.Infrastructure.Persistence;

namespace StudentCouncil.UnitTests.Validation;

public class EventValidatorTests
{
    private static readonly Guid DepartmentId = Guid.NewGuid();
    private static readonly Guid ActiveMember = Guid.NewGuid();
    private static readonly Guid InactiveMember = Guid.NewGuid();
    private static readonly DateTime Start = new(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc);

    private static async Task<AppDbContext> BuildDbAsync()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"events-{Guid.NewGuid()}")
            .Options);

        db.Departments.Add(new Department { Id = DepartmentId, Code = DepartmentCode.PR, Name = "Public Relations" });
        db.Users.Add(Member(ActiveMember, "ev.active@ue-varna.bg", MemberStatus.Active));
        db.Users.Add(Member(InactiveMember, "ev.inactive@ue-varna.bg", MemberStatus.Inactive));
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
        DepartmentId = DepartmentId,
        Status = status
    };

    private static CreateEventCommand Create(
        DateTime startUtc, DateTime endUtc, Guid? departmentId = null,
        IReadOnlyList<Guid>? participants = null, string title = "Team meeting") =>
        new(title, null, startUtc, endUtc, null, EventType.Meeting, departmentId, RecurrenceType.None, participants);

    private static async Task<CreateEventValidator> ValidatorAsync(AppDbContext db) =>
        await Task.FromResult(new CreateEventValidator(db, new MemberDirectory(db)));

    [Fact]
    public async Task End_before_start_is_rejected()
    {
        await using var db = await BuildDbAsync();
        var validator = await ValidatorAsync(db);

        var result = await validator.ValidateAsync(Create(Start, Start.AddHours(-1)));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Non_existent_department_is_rejected()
    {
        await using var db = await BuildDbAsync();
        var validator = await ValidatorAsync(db);

        var result = await validator.ValidateAsync(Create(Start, Start.AddHours(1), Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Inactive_participant_is_rejected()
    {
        await using var db = await BuildDbAsync();
        var validator = await ValidatorAsync(db);

        var result = await validator.ValidateAsync(Create(Start, Start.AddHours(1), participants: [InactiveMember]));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Short_title_is_rejected()
    {
        await using var db = await BuildDbAsync();
        var validator = await ValidatorAsync(db);

        var result = await validator.ValidateAsync(Create(Start, Start.AddHours(1), title: "Hi"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task A_well_formed_event_passes()
    {
        await using var db = await BuildDbAsync();
        var validator = await ValidatorAsync(db);

        var result = await validator.ValidateAsync(
            Create(Start, Start.AddHours(1), DepartmentId, [ActiveMember]));

        result.IsValid.Should().BeTrue();
    }
}
