using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Features.Members;
using StudentCouncil.Domain.Entities;
using StudentCouncil.Domain.Enums;
using StudentCouncil.Infrastructure.Identity;
using StudentCouncil.Infrastructure.Persistence;

namespace StudentCouncil.UnitTests.Validation;

public class CreateMemberValidatorTests
{
    private static readonly Guid PrDepartmentId = Guid.NewGuid();

    private static async Task<(CreateMemberValidator Validator, AppDbContext Db)> CreateAsync()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"members-{Guid.NewGuid()}")
            .Options);

        db.Departments.Add(new Department { Id = PrDepartmentId, Code = DepartmentCode.PR, Name = "Public Relations" });
        db.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "exists@ue-varna.bg",
            UserName = "exists@ue-varna.bg",
            FullName = "Existing Member",
            Role = SystemRole.Member,
            DepartmentId = PrDepartmentId,
            Status = MemberStatus.Active
        });
        await db.SaveChangesAsync();

        return (new CreateMemberValidator(new MemberDirectory(db), db), db);
    }

    private static CreateMemberCommand Command(SystemRole role, Guid? departmentId, string email = "new@ue-varna.bg") =>
        new("New Member", email, null, role, departmentId, new DateOnly(2026, 1, 1));

    [Fact]
    public async Task Org_role_must_not_have_a_department()
    {
        var (validator, _) = await CreateAsync();

        var result = await validator.ValidateAsync(Command(SystemRole.OrgSecretary, PrDepartmentId));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateMemberCommand.DepartmentId));
    }

    [Fact]
    public async Task Dept_role_requires_a_department()
    {
        var (validator, _) = await CreateAsync();

        var result = await validator.ValidateAsync(Command(SystemRole.Member, departmentId: null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Non_existent_department_is_rejected()
    {
        var (validator, _) = await CreateAsync();

        var result = await validator.ValidateAsync(Command(SystemRole.Member, Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Duplicate_email_is_rejected()
    {
        var (validator, _) = await CreateAsync();

        var result = await validator.ValidateAsync(Command(SystemRole.Member, PrDepartmentId, "EXISTS@ue-varna.bg"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateMemberCommand.Email));
    }

    [Fact]
    public async Task Valid_member_passes()
    {
        var (validator, _) = await CreateAsync();

        var result = await validator.ValidateAsync(Command(SystemRole.Member, PrDepartmentId));

        result.IsValid.Should().BeTrue();
    }
}
