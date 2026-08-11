using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace StudentCouncil.IntegrationTests;

public class AuthorizationTests : IClassFixture<RecordingEmailFactory>
{
    private readonly RecordingEmailFactory _factory;

    public AuthorizationTests(RecordingEmailFactory factory) => _factory = factory;

    [Fact]
    public async Task Endpoints_enforce_authentication_and_role()
    {
        var client = _factory.CreateClient();

        // Unauthenticated access is rejected.
        (await client.GetAsync("/api/v1/members")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Admin creates a plain Member.
        await Api.AuthenticateAdminAsync(client);
        var departmentId = await Api.GetDepartmentIdAsync(client, "Social");

        const string memberEmail = "matrix.member@ue-varna.bg";
        var create = await client.PostAsJsonAsync("/api/v1/members", new
        {
            fullName = "Matrix Member",
            email = memberEmail,
            phoneNumber = (string?)null,
            role = "Member",
            departmentId,
            joinedOn = "2026-02-01"
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        // The member clears the password gate so role checks (not the gate) apply.
        var temporaryPassword = Api.ExtractTemporaryPassword(_factory.Email, memberEmail);
        Api.ClearBearer(client);
        var firstLogin = await Api.LoginAsync(client, memberEmail, temporaryPassword);
        Api.UseBearer(client, firstLogin.AccessToken);
        var memberChange = await client.PostAsJsonAsync("/api/v1/auth/change-password",
            new { currentPassword = temporaryPassword, newPassword = "MemberPass1" });
        memberChange.StatusCode.Should().Be(HttpStatusCode.NoContent);

        Api.ClearBearer(client);
        var session = await Api.LoginAsync(client, memberEmail, "MemberPass1");
        Api.UseBearer(client, session.AccessToken);

        // An authenticated read is allowed...
        (await client.GetAsync("/api/v1/members")).StatusCode.Should().Be(HttpStatusCode.OK);

        // ...but a management write is forbidden for a plain Member.
        var forbidden = await client.PostAsJsonAsync("/api/v1/members", new
        {
            fullName = "Should Not Be Created",
            email = "nope@ue-varna.bg",
            phoneNumber = (string?)null,
            role = "Member",
            departmentId,
            joinedOn = "2026-02-01"
        });
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
