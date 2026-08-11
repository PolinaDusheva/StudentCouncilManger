using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace StudentCouncil.IntegrationTests;

public class PasswordChangeGateTests : IClassFixture<SmokeTestFactory>
{
    private readonly SmokeTestFactory _factory;

    public PasswordChangeGateTests(SmokeTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Pending_change_blocks_everything_except_the_allowed_endpoints()
    {
        var client = _factory.CreateClient();

        var login = await Api.LoginAsync(client, SmokeTestFactory.AdminEmail, SmokeTestFactory.AdminInitialPassword);
        login.MustChangePassword.Should().BeTrue();
        Api.UseBearer(client, login.AccessToken);

        // A normal endpoint is gated with the specific code.
        var gated = await client.GetAsync("/api/v1/members");
        gated.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Api.ReadCodeAsync(gated)).Should().Be("password_change_required");

        // /auth/me is allowed while the change is pending.
        (await client.GetAsync("/api/v1/auth/me")).StatusCode.Should().Be(HttpStatusCode.OK);

        // change-password is allowed and clears the gate.
        var change = await client.PostAsJsonAsync("/api/v1/auth/change-password",
            new { currentPassword = SmokeTestFactory.AdminInitialPassword, newPassword = "ChangedAdmin1" });
        change.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
