using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentCouncil.Application.Abstractions;

namespace StudentCouncil.IntegrationTests;

// Fresh factory (database + rate-limit window) per test, matching the one-scenario-per-factory pattern.
public class DevicesTests : IAsyncLifetime
{
    private readonly RecordingNotificationsFactory _factory = new();

    public Task InitializeAsync() => _factory.InitializeAsync();

    public Task DisposeAsync() => ((IAsyncLifetime)_factory).DisposeAsync();

    private static object Device(string token, string platform = "Android") => new { token, platform };

    [Fact]
    public async Task Register_is_an_upsert_keyed_on_the_token()
    {
        var client = _factory.CreateClient();
        await Api.AuthenticateAdminAsync(client);

        // First registration creates a row.
        var first = await client.PostAsJsonAsync("/api/v1/devices", Device("token-abc"));
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await first.Content.ReadFromJsonAsync<DeviceRegistrationResponse>())!;
        created.Created.Should().BeTrue();

        // Re-registering the same token refreshes the existing row (same id, 200, not a duplicate).
        var second = await client.PostAsJsonAsync("/api/v1/devices", Device("token-abc", "iOS"));
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = (await second.Content.ReadFromJsonAsync<DeviceRegistrationResponse>())!;
        refreshed.Created.Should().BeFalse();
        refreshed.Id.Should().Be(created.Id);

        (await CountTokensAsync("token-abc")).Should().Be(1);
    }

    [Fact]
    public async Task Deregister_is_idempotent_and_only_removes_the_callers_token()
    {
        var client = _factory.CreateClient();
        var adminToken = await Api.AuthenticateAdminAsync(client);
        var prId = await Api.GetDepartmentIdAsync(client, "PR");

        const string adminToken1 = "admin-device";
        (await client.PostAsJsonAsync("/api/v1/devices", Device(adminToken1)))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        // A second member cannot delete the admin's token (silent no-op, still 204).
        await TaskApi.CreateMemberAsync(client, "Dev Other", "dev.other@ue-varna.bg", "Member", prId);
        await TaskApi.SignInFreshMemberAsync(client, _factory.Email, "dev.other@ue-varna.bg", "DevOtherPass1");
        (await client.DeleteAsync($"/api/v1/devices/{adminToken1}"))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await CountTokensAsync(adminToken1)).Should().Be(1, "a foreign token must not be deleted");

        // The owner can delete it, and a repeat delete is still 204 (idempotent).
        Api.UseBearer(client, adminToken);
        (await client.DeleteAsync($"/api/v1/devices/{adminToken1}"))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await CountTokensAsync(adminToken1)).Should().Be(0);
        (await client.DeleteAsync($"/api/v1/devices/{adminToken1}"))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Register_rejects_an_empty_token_or_unknown_platform()
    {
        var client = _factory.CreateClient();
        await Api.AuthenticateAdminAsync(client);

        (await client.PostAsJsonAsync("/api/v1/devices", new { token = "", platform = "Android" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync("/api/v1/devices", new { token = "ok", platform = "Windows" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<int> CountTokensAsync(string token)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        return await db.DeviceTokens.CountAsync(d => d.Token == token);
    }
}

public sealed record DeviceRegistrationResponse(Guid Id, bool Created);
