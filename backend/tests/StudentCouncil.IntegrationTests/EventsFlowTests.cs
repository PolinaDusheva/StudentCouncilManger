using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace StudentCouncil.IntegrationTests;

// A fresh factory (database + rate-limit window) per test: each scenario re-seeds the admin and
// stays under the per-IP auth rate limit, matching the one-scenario-per-factory pattern.
public class EventsFlowTests : IAsyncLifetime
{
    private readonly RecordingEmailFactory _factory = new();

    public Task InitializeAsync() => _factory.InitializeAsync();

    public Task DisposeAsync() => ((IAsyncLifetime)_factory).DisposeAsync();

    private const string Window = "?from=2026-07-01T00:00:00Z&to=2026-07-31T23:59:59Z";

    [Fact]
    public async Task Create_list_recur_deadline_ics_and_conflict_end_to_end()
    {
        var client = _factory.CreateClient();
        var adminToken = await Api.AuthenticateAdminAsync(client);
        var prId = await Api.GetDepartmentIdAsync(client, "PR");

        // A visible task with a July due date should surface as a virtual Deadline entry.
        var assigneeId = await TaskApi.CreateMemberAsync(client, "Ev Assignee", "ev.flow.assignee@ue-varna.bg", "Member", prId);
        var createTask = await client.PostAsJsonAsync("/api/v1/tasks", new
        {
            title = "Submit report",
            priority = "High",
            scope = "Departmental",
            departmentId = prId,
            dueAtUtc = "2026-07-15T12:00:00Z",
            assigneeIds = new[] { assigneeId }
        });
        createTask.StatusCode.Should().Be(HttpStatusCode.Created);
        var taskId = (await createTask.Content.ReadFromJsonAsync<TaskDetailResponse>())!.Id;

        // Create a one-off event; no conflicts yet.
        var firstCreate = await client.PostAsJsonAsync("/api/v1/events", new
        {
            title = "Planning meeting",
            description = "Quarterly planning",
            startUtc = "2026-07-10T09:00:00Z",
            endUtc = "2026-07-10T10:00:00Z",
            location = "Room 101",
            type = "Meeting",
            departmentId = prId,
            recurrence = "None"
        });
        firstCreate.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstResult = (await firstCreate.Content.ReadFromJsonAsync<EventMutationResponse>())!;
        firstResult.ConflictsWith.Should().BeEmpty();
        firstResult.Event.Department.Should().Be("PR");
        firstResult.Event.Organizer.Should().NotBeNull();
        var firstEventId = firstResult.Event.Id;

        // A weekly event expands into several occurrences inside the window.
        var weeklyCreate = await client.PostAsJsonAsync("/api/v1/events", new
        {
            title = "Weekly standup",
            startUtc = "2026-07-01T08:00:00Z",
            endUtc = "2026-07-01T08:30:00Z",
            type = "InternalMeeting",
            recurrence = "Weekly"
        });
        weeklyCreate.StatusCode.Should().Be(HttpStatusCode.Created);

        var events = await client.GetFromJsonAsync<List<EventResponse>>($"/api/v1/events{Window}");
        events!.Should().Contain(e => e.Id == firstEventId && !e.IsDeadline);

        var deadline = events.Should().ContainSingle(e => e.IsDeadline).Subject;
        deadline.TaskId.Should().Be(taskId);
        deadline.Type.Should().Be("Deadline");

        var standups = events.Where(e => e.Title == "Weekly standup").ToList();
        standups.Should().HaveCountGreaterThanOrEqualTo(4);
        standups.Should().OnlyContain(e => e.OccurrenceStartUtc != null && e.Recurrence == "Weekly");

        // The .ics export carries both the real event and the task deadline.
        var ics = await client.GetAsync($"/api/v1/events/export.ics{Window}");
        ics.StatusCode.Should().Be(HttpStatusCode.OK);
        ics.Content.Headers.ContentType!.MediaType.Should().Be("text/calendar");
        var icsBody = await ics.Content.ReadAsStringAsync();
        icsBody.Should().Contain("BEGIN:VCALENDAR");
        icsBody.Should().Contain($"task-{taskId}@");
        icsBody.Should().Contain($"{firstEventId}@");

        // An overlapping event comes back with a non-blocking conflict warning.
        var overlapCreate = await client.PostAsJsonAsync("/api/v1/events", new
        {
            title = "Overlapping meeting",
            startUtc = "2026-07-10T09:30:00Z",
            endUtc = "2026-07-10T10:30:00Z",
            type = "Meeting",
            recurrence = "None"
        });
        overlapCreate.StatusCode.Should().Be(HttpStatusCode.Created);
        var overlapResult = (await overlapCreate.Content.ReadFromJsonAsync<EventMutationResponse>())!;
        overlapResult.ConflictsWith.Should().Contain(c => c.Id == firstEventId);
    }

    [Fact]
    public async Task End_before_start_is_rejected()
    {
        var client = _factory.CreateClient();
        await Api.AuthenticateAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/events", new
        {
            title = "Backwards event",
            startUtc = "2026-07-10T10:00:00Z",
            endUtc = "2026-07-10T09:00:00Z",
            type = "Meeting",
            recurrence = "None"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
