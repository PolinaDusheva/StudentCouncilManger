using FluentAssertions;
using StudentCouncil.Application.Common.Security;
using StudentCouncil.Domain.Enums;

namespace StudentCouncil.UnitTests.Security;

public class MemberPermissionsTests
{
    [Fact]
    public void OrgPresident_has_every_permission()
    {
        var permissions = MemberPermissions.For(SystemRole.OrgPresident);

        permissions.Should().BeEquivalentTo(new PermissionSet(true, true, true, true, true, true));
    }

    [Fact]
    public void DeptPresident_can_create_department_tasks_and_manage_events()
    {
        var permissions = MemberPermissions.For(SystemRole.DeptPresident);

        permissions.Should().BeEquivalentTo(new PermissionSet(false, false, false, false, true, true));
    }

    [Theory]
    [InlineData(SystemRole.DeptSecretary)]
    [InlineData(SystemRole.OrgSecretary)]
    public void Secretaries_can_manage_events_but_not_tasks(SystemRole role)
    {
        var permissions = MemberPermissions.For(role);

        permissions.Should().BeEquivalentTo(new PermissionSet(false, false, false, false, false, true));
    }

    [Fact]
    public void Member_has_no_management_permissions()
    {
        var permissions = MemberPermissions.For(SystemRole.Member);

        permissions.Should().BeEquivalentTo(new PermissionSet(false, false, false, false, false, false));
    }
}
