using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace StudentCouncil.IntegrationTests;

public class DeactivationTests : IClassFixture<RecordingEmailFactory>
{
    private readonly RecordingEmailFactory _factory;

    public DeactivationTests(RecordingEmailFactory factory) => _factory = factory;

    [Fact]
    public async Task Deactivation_invalidates_existing_tokens_and_blocks_login()
    {
        var client = _factory.CreateClient();

        var adminToken = await Api.AuthenticateAdminAsync(client);
        var departmentId = await Api.GetDepartmentIdAsync(client, "Sports");

        const string memberEmail = "leaver@ue-varna.bg";
        var create = await client.PostAsJsonAsync("/api/v1/members", new
        {
            fullName = "Leaving Member",
            email = memberEmail,
            phoneNumber = (string?)null,
            role = "Member",
            departmentId,
            joinedOn = "2026-03-01"
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var member = (await create.Content.ReadFromJsonAsync<MemberResponse>())!;

        // The member signs in and holds an access token.
        var temporaryPassword = Api.ExtractTemporaryPassword(_factory.Email, memberEmail);
        Api.ClearBearer(client);
        var memberSession = await Api.LoginAsync(client, memberEmail, temporaryPassword);

        // Admin deactivates the member.
        Api.UseBearer(client, adminToken);
        var deactivate = await client.PostAsync($"/api/v1/members/{member.Id}/deactivate", content: null);
        deactivate.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // The member's previously-issued access token is now rejected (security stamp / status).
        Api.UseBearer(client, memberSession.AccessToken);
        (await client.GetAsync("/api/v1/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // And they can no longer sign in.
        Api.ClearBearer(client);
        var relogin = await Api.LoginRawAsync(client, memberEmail, temporaryPassword);
        relogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await Api.ReadCodeAsync(relogin)).Should().Be("account_inactive");
    }
}
