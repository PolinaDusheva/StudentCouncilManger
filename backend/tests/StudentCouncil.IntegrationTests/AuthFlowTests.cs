using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace StudentCouncil.IntegrationTests;

public class AuthFlowTests : IClassFixture<RecordingEmailFactory>
{
    private readonly RecordingEmailFactory _factory;

    public AuthFlowTests(RecordingEmailFactory factory) => _factory = factory;

    [Fact]
    public async Task Full_flow_admin_bootstraps_then_creates_and_a_member_signs_in()
    {
        var client = _factory.CreateClient();

        // 1) First admin login must signal the forced password change.
        var firstLogin = await Api.LoginAsync(client, SmokeTestFactory.AdminEmail, SmokeTestFactory.AdminInitialPassword);
        firstLogin.MustChangePassword.Should().BeTrue();
        firstLogin.User.Role.Should().Be("OrgPresident");

        // 2) Change password, then sign back in cleanly.
        await Api.AuthenticateAdminAsync(client);

        // 3) Departments are seeded and visible.
        var prDepartmentId = await Api.GetDepartmentIdAsync(client, "PR");

        // 4) Create a member.
        const string memberEmail = "new.member@ue-varna.bg";
        var createResponse = await client.PostAsJsonAsync("/api/v1/members", new
        {
            fullName = "New Member",
            email = memberEmail,
            phoneNumber = (string?)null,
            role = "Member",
            departmentId = prDepartmentId,
            joinedOn = "2026-01-15"
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<MemberResponse>())!;
        created.Department.Should().Be("PR");

        // 5) The new member shows up in the list.
        var list = await client.GetFromJsonAsync<JsonPagedMembers>("/api/v1/members?search=New%20Member");
        list!.Items.Should().Contain(m => m.Id == created.Id);

        // 6) The member can sign in with the emailed temporary password.
        var temporaryPassword = Api.ExtractTemporaryPassword(_factory.Email, memberEmail);
        Api.ClearBearer(client);
        var memberLogin = await Api.LoginAsync(client, memberEmail, temporaryPassword);
        memberLogin.MustChangePassword.Should().BeTrue();

        // 7) /auth/me works while the change is pending and reports no management permissions.
        Api.UseBearer(client, memberLogin.AccessToken);
        var me = await client.GetFromJsonAsync<MeDto>("/api/v1/auth/me");
        me!.Role.Should().Be("Member");
        me.Department.Should().Be("PR");
        me.Permissions.CanManageMembers.Should().BeFalse();
        me.Permissions.CanCreateDeptTask.Should().BeFalse();
    }

    private sealed record JsonPagedMembers(List<MemberResponse> Items, int Page, int PageSize, int TotalCount);
}
