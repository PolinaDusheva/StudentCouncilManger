using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace StudentCouncil.IntegrationTests;

// Fresh factory (database + rate-limit window) per test, matching the one-scenario-per-factory pattern.
public class DutyRecordsTests : IAsyncLifetime
{
    private readonly RecordingEmailFactory _factory = new();

    public Task InitializeAsync() => _factory.InitializeAsync();

    public Task DisposeAsync() => ((IAsyncLifetime)_factory).DisposeAsync();

    private static object DutyBody(Guid memberId, string startUtc, string endUtc, string? note = null) =>
        new { memberId, startUtc, endUtc, note };

    [Fact]
    public async Task Member_sees_only_their_own_duties_and_cannot_manage()
    {
        var client = _factory.CreateClient();
        var adminToken = await Api.AuthenticateAdminAsync(client);
        var prId = await Api.GetDepartmentIdAsync(client, "PR");

        var memberA = await TaskApi.CreateMemberAsync(client, "Duty A", "duty.a@ue-varna.bg", "Member", prId);
        var memberB = await TaskApi.CreateMemberAsync(client, "Duty B", "duty.b@ue-varna.bg", "Member", prId);

        Api.UseBearer(client, adminToken);
        (await client.PostAsJsonAsync("/api/v1/duty-records",
            DutyBody(memberA, "2026-06-10T09:00:00Z", "2026-06-10T11:00:00Z"))).StatusCode.Should().Be(HttpStatusCode.Created);
        (await client.PostAsJsonAsync("/api/v1/duty-records",
            DutyBody(memberB, "2026-06-11T09:00:00Z", "2026-06-11T10:00:00Z"))).StatusCode.Should().Be(HttpStatusCode.Created);

        await TaskApi.SignInFreshMemberAsync(client, _factory.Email, "duty.a@ue-varna.bg", "DutyAPass1");

        // The member sees only their own records, even though B's exists.
        var mine = await client.GetFromJsonAsync<List<DutyRecordResponse>>("/api/v1/duty-records");
        mine!.Should().OnlyContain(d => d.Member!.Id == memberA);
        mine.Should().ContainSingle();

        // The supplied memberId is ignored for a plain member (cannot peek at B).
        var spoof = await client.GetFromJsonAsync<List<DutyRecordResponse>>($"/api/v1/duty-records?memberId={memberB}");
        spoof!.Should().OnlyContain(d => d.Member!.Id == memberA);

        // No management rights: cannot view the summary or register duties.
        (await client.GetAsync("/api/v1/duty-records/summary")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.PostAsJsonAsync("/api/v1/duty-records",
            DutyBody(memberA, "2026-06-12T09:00:00Z", "2026-06-12T10:00:00Z"))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Summary_me_summary_and_remind_resolve_correctly()
    {
        var client = _factory.CreateClient();
        var adminToken = await Api.AuthenticateAdminAsync(client);
        var prId = await Api.GetDepartmentIdAsync(client, "PR");

        var memberA = await TaskApi.CreateMemberAsync(client, "Duty Star", "duty.star@ue-varna.bg", "Member", prId);

        Api.UseBearer(client, adminToken);
        // Exactly the 120-minute norm for June 2026.
        (await client.PostAsJsonAsync("/api/v1/duty-records",
            DutyBody(memberA, "2026-06-10T09:00:00Z", "2026-06-10T11:00:00Z"))).StatusCode.Should().Be(HttpStatusCode.Created);

        var summary = await client.GetFromJsonAsync<List<DutySummaryRowResponse>>("/api/v1/duty-records/summary?year=2026&month=6");
        var starRow = summary!.Single(r => r.Member.Id == memberA);
        starRow.TotalMinutes.Should().Be(120);
        starRow.RequiredMinutes.Should().Be(120);
        starRow.MetNorm.Should().BeTrue();

        // A targeted reminder resolves to exactly the one active member.
        var targeted = await client.PostAsJsonAsync("/api/v1/duty-records/remind", new { memberId = memberA });
        targeted.StatusCode.Should().Be(HttpStatusCode.OK);
        (await targeted.Content.ReadFromJsonAsync<RemindResponse>())!.Notified.Should().Be(1);

        // A broadcast reminder resolves to everyone under norm (at least the seeded admin, who has none).
        var broadcast = await client.PostAsJsonAsync("/api/v1/duty-records/remind", new { });
        broadcast.StatusCode.Should().Be(HttpStatusCode.OK);
        (await broadcast.Content.ReadFromJsonAsync<RemindResponse>())!.Notified.Should().BeGreaterThanOrEqualTo(1);

        // The member's own summary mirrors the table row.
        await TaskApi.SignInFreshMemberAsync(client, _factory.Email, "duty.star@ue-varna.bg", "DutyStarPass1");
        var mine = await client.GetFromJsonAsync<MyDutySummaryResponse>("/api/v1/duty-records/me/summary?year=2026&month=6");
        mine!.TotalMinutes.Should().Be(120);
        mine.MetNorm.Should().BeTrue();
    }

    [Fact]
    public async Task Invalid_duties_are_rejected()
    {
        var client = _factory.CreateClient();
        await Api.AuthenticateAdminAsync(client);
        var prId = await Api.GetDepartmentIdAsync(client, "PR");

        var active = await TaskApi.CreateMemberAsync(client, "Active One", "duty.active1@ue-varna.bg", "Member", prId);
        var toDeactivate = await TaskApi.CreateMemberAsync(client, "Gone Soon", "duty.gone@ue-varna.bg", "Member", prId);
        (await client.PostAsync($"/api/v1/members/{toDeactivate}/deactivate", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        // End before start.
        (await client.PostAsJsonAsync("/api/v1/duty-records",
            DutyBody(active, "2026-06-10T11:00:00Z", "2026-06-10T09:00:00Z"))).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Inactive member.
        (await client.PostAsJsonAsync("/api/v1/duty-records",
            DutyBody(toDeactivate, "2026-06-10T09:00:00Z", "2026-06-10T11:00:00Z"))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
