using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Features.Tasks;
using StudentCouncil.Application.Features.Tasks.Comments;
using StudentCouncil.Domain.Entities;
using StudentCouncil.Domain.Enums;
using StudentCouncil.Infrastructure.Identity;
using StudentCouncil.Infrastructure.Persistence;

namespace StudentCouncil.UnitTests.Validation;

public class TaskValidatorTests
{
    private static readonly Guid DepartmentId = Guid.NewGuid();
    private static readonly Guid ActiveMember = Guid.NewGuid();
    private static readonly Guid InactiveMember = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 6, 27, 0, 0, 0, DateTimeKind.Utc);

    private static async Task<AppDbContext> BuildDbAsync()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"tasks-{Guid.NewGuid()}")
            .Options);

        db.Departments.Add(new Department { Id = DepartmentId, Code = DepartmentCode.PR, Name = "Public Relations" });
        db.Users.Add(Member(ActiveMember, "active@ue-varna.bg", MemberStatus.Active));
        db.Users.Add(Member(InactiveMember, "inactive@ue-varna.bg", MemberStatus.Inactive));
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

    private static IDateTime Clock()
    {
        var clock = Substitute.For<IDateTime>();
        clock.UtcNow.Returns(Now);
        return clock;
    }

    private static CreateTaskCommand Create(
        TaskScope scope, Guid? departmentId, IReadOnlyList<Guid> assignees, DateTime? dueAt = null, string title = "Plan event") =>
        new(title, null, TaskPriority.Medium, scope, departmentId, dueAt, assignees);

    [Fact]
    public async Task Departmental_task_requires_a_department()
    {
        await using var db = await BuildDbAsync();
        var validator = new CreateTaskValidator(db, new MemberDirectory(db), Clock());

        var result = await validator.ValidateAsync(Create(TaskScope.Departmental, null, [ActiveMember]));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Organisational_task_must_not_have_a_department()
    {
        await using var db = await BuildDbAsync();
        var validator = new CreateTaskValidator(db, new MemberDirectory(db), Clock());

        var result = await validator.ValidateAsync(Create(TaskScope.Organizational, DepartmentId, [ActiveMember]));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Non_existent_department_is_rejected()
    {
        await using var db = await BuildDbAsync();
        var validator = new CreateTaskValidator(db, new MemberDirectory(db), Clock());

        var result = await validator.ValidateAsync(Create(TaskScope.Departmental, Guid.NewGuid(), [ActiveMember]));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task At_least_one_assignee_is_required()
    {
        await using var db = await BuildDbAsync();
        var validator = new CreateTaskValidator(db, new MemberDirectory(db), Clock());

        var result = await validator.ValidateAsync(Create(TaskScope.Departmental, DepartmentId, []));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Inactive_assignees_are_rejected()
    {
        await using var db = await BuildDbAsync();
        var validator = new CreateTaskValidator(db, new MemberDirectory(db), Clock());

        var result = await validator.ValidateAsync(Create(TaskScope.Departmental, DepartmentId, [InactiveMember]));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Due_date_in_the_past_is_rejected()
    {
        await using var db = await BuildDbAsync();
        var validator = new CreateTaskValidator(db, new MemberDirectory(db), Clock());

        var result = await validator.ValidateAsync(
            Create(TaskScope.Departmental, DepartmentId, [ActiveMember], Now.AddDays(-1)));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Short_title_is_rejected()
    {
        await using var db = await BuildDbAsync();
        var validator = new CreateTaskValidator(db, new MemberDirectory(db), Clock());

        var result = await validator.ValidateAsync(
            Create(TaskScope.Departmental, DepartmentId, [ActiveMember], title: "Hi"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task A_well_formed_departmental_task_passes()
    {
        await using var db = await BuildDbAsync();
        var validator = new CreateTaskValidator(db, new MemberDirectory(db), Clock());

        var result = await validator.ValidateAsync(
            Create(TaskScope.Departmental, DepartmentId, [ActiveMember], Now.AddDays(7)));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Update_rejects_inactive_assignees_and_accepts_active_ones()
    {
        await using var db = await BuildDbAsync();
        var validator = new UpdateTaskValidator(new MemberDirectory(db));

        var invalid = await validator.ValidateAsync(
            new UpdateTaskCommand(Guid.NewGuid(), "Updated title", null, TaskPriority.High, null, [InactiveMember]));
        var valid = await validator.ValidateAsync(
            new UpdateTaskCommand(Guid.NewGuid(), "Updated title", null, TaskPriority.High, null, [ActiveMember]));

        invalid.IsValid.Should().BeFalse();
        valid.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("A reasonable comment", true)]
    public void Comment_text_must_be_present(string text, bool expected)
    {
        var validator = new AddTaskCommentValidator();

        validator.Validate(new AddTaskCommentCommand(Guid.NewGuid(), text)).IsValid.Should().Be(expected);
    }

    [Fact]
    public void Comment_text_over_2000_chars_is_rejected()
    {
        var validator = new AddTaskCommentValidator();

        var result = validator.Validate(new AddTaskCommentCommand(Guid.NewGuid(), new string('x', 2001)));

        result.IsValid.Should().BeFalse();
    }
}
