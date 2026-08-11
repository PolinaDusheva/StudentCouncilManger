using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace StudentCouncil.IntegrationTests;

// Fresh factory (database + rate-limit window) per test, matching the one-scenario-per-factory pattern.
public class CalendarAuthorizationTests : IAsyncLifetime
{
    private readonly RecordingEmailFactory _factory = new();

    public Task InitializeAsync() => _factory.InitializeAsync();

    public Task DisposeAsync() => ((IAsyncLifetime)_factory).DisposeAsync();

    private static object EventBody(
        string title, string startUtc, string endUtc, Guid? departmentId = null, string type = "Meeting") => new
    {
        title,
        description = (string?)null,
        startUtc,
        endUtc,
        location = (string?)null,
        type,
        departmentId,
        recurrence = "None",
        participantIds = (Guid[]?)null
    };

    [Fact]
    public async Task Member_cannot_create_an_event()
    {
        var client = _factory.CreateClient();
        await Api.AuthenticateAdminAsync(client);
        var prId = await Api.GetDepartmentIdAsync(client, "PR");

        await TaskApi.CreateMemberAsync(client, "Plain Member", "ev.plain@ue-varna.bg", "Member", prId);
        await TaskApi.SignInFreshMemberAsync(client, _factory.Email, "ev.plain@ue-varna.bg", "PlainPass1");

        var create = await client.PostAsJsonAsync("/api/v1/events",
            EventBody("Member event", "2026-07-10T09:00:00Z", "2026-07-10T10:00:00Z", prId));

        create.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Secretary_can_edit_own_event_but_not_delete_or_touch_another_department()
    {
        var client = _factory.CreateClient();
        await Api.AuthenticateAdminAsync(client);
        var prId = await Api.GetDepartmentIdAsync(client, "PR");
        var sportsId = await Api.GetDepartmentIdAsync(client, "Sports");

        await TaskApi.CreateMemberAsync(client, "PR Secretary", "ev.secretary@ue-varna.bg", "DeptSecretary", prId);

        // Admin creates a Sports-department event (organised by admin, not the PR secretary).
        var sportsCreate = await client.PostAsJsonAsync("/api/v1/events",
            EventBody("Sports gala", "2026-07-12T18:00:00Z", "2026-07-12T20:00:00Z", sportsId, "SportsEvent"));
        sportsCreate.StatusCode.Should().Be(HttpStatusCode.Created);
        var sportsEventId = (await sportsCreate.Content.ReadFromJsonAsync<EventMutationResponse>())!.Event.Id;

        // The PR secretary organises their own event.
        await TaskApi.SignInFreshMemberAsync(client, _factory.Email, "ev.secretary@ue-varna.bg", "SecretaryPass1");
        var ownCreate = await client.PostAsJsonAsync("/api/v1/events",
            EventBody("PR briefing", "2026-07-11T09:00:00Z", "2026-07-11T10:00:00Z", prId));
        ownCreate.StatusCode.Should().Be(HttpStatusCode.Created);
        var ownEventId = (await ownCreate.Content.ReadFromJsonAsync<EventMutationResponse>())!.Event.Id;

        // Owner can edit their own event...
        var edit = await client.PutAsJsonAsync($"/api/v1/events/{ownEventId}",
            EventBody("PR briefing (updated)", "2026-07-11T09:00:00Z", "2026-07-11T10:30:00Z", prId));
        edit.StatusCode.Should().Be(HttpStatusCode.OK);

        // ...but a secretary cannot delete, even their own event.
        (await client.DeleteAsync($"/api/v1/events/{ownEventId}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // ...and cannot edit another department's event.
        var crossEdit = await client.PutAsJsonAsync($"/api/v1/events/{sportsEventId}",
            EventBody("Hijacked gala", "2026-07-12T18:00:00Z", "2026-07-12T21:00:00Z", sportsId, "SportsEvent"));
        crossEdit.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
