using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StudentCouncil.Domain.Enums;

namespace StudentCouncil.IntegrationTests;

// Fresh factory per test (isolated database + push recorder), matching the one-scenario-per-factory pattern.
public class NotificationsTests : IAsyncLifetime
{
    private readonly RecordingNotificationsFactory _factory = new();

    public Task InitializeAsync() => _factory.InitializeAsync();

    public Task DisposeAsync() => ((IAsyncLifetime)_factory).DisposeAsync();

    [Fact]
    public async Task Actions_create_in_app_notifications_for_the_right_recipients()
    {
        var client = _factory.CreateClient();
        var adminToken = await Api.AuthenticateAdminAsync(client);
        var socialId = await Api.GetDepartmentIdAsync(client, "Social");

        const string assigneeEmail = "notif.assignee@ue-varna.bg";
        var assigneeId = await TaskApi.CreateMemberAsync(client, "Notif Assignee", assigneeEmail, "Member", socialId);

        // Org leadership assigns a task to the member → TaskAssigned notification.
        Api.UseBearer(client, adminToken);
        var create = await client.PostAsJsonAsync("/api/v1/tasks", new
        {
            title = "Prepare posters",
            description = (string?)null,
            priority = "Medium",
            scope = "Departmental",
            departmentId = socialId,
            dueAtUtc = "2026-12-01T10:00:00Z",
            assigneeIds = new[] { assigneeId }
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var taskId = (await create.Content.ReadFromJsonAsync<TaskDetailResponse>())!.Id;

        // Push was dispatched to the assignee for the assignment.
        _factory.Push.Sent.Should().Contain(p =>
            p.Type == NotificationType.TaskAssigned && p.MemberIds.Contains(assigneeId));

        // The assignee sees exactly the TaskAssigned entry, deep-linked to the task.
        var assigneeToken = await TaskApi.SignInFreshMemberAsync(client, _factory.Email, assigneeEmail, "NotifAssignee1");
        var unread = await GetAsync(client, unreadOnly: true);
        unread.Items.Should().ContainSingle();
        var assigned = unread.Items.Single();
        assigned.Type.Should().Be("TaskAssigned");
        assigned.IsRead.Should().BeFalse();
        assigned.Payload!.Type.Should().Be("Task");
        assigned.Payload.Id.Should().Be(taskId);

        // A foreign user cannot mark it read — it surfaces as 404, not 403.
        Api.UseBearer(client, adminToken);
        (await client.PostAsync($"/api/v1/notifications/{assigned.Id}/read", null))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);

        // The owner marks it read; the unread list empties.
        Api.UseBearer(client, assigneeToken);
        (await client.PostAsync($"/api/v1/notifications/{assigned.Id}/read", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await GetAsync(client, unreadOnly: true)).Items.Should().BeEmpty();

        // A comment by the admin notifies the assignee (a participant who is not the author).
        Api.UseBearer(client, adminToken);
        (await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/comments", new { text = "Any progress?" }))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        Api.UseBearer(client, assigneeToken);
        var afterComment = await GetAsync(client, unreadOnly: true);
        afterComment.Items.Should().ContainSingle(n => n.Type == "TaskComment");

        // read-all clears everything; the full list still shows both entries, now read.
        (await client.PostAsync("/api/v1/notifications/read-all", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await GetAsync(client, unreadOnly: true)).Items.Should().BeEmpty();

        var all = await GetAsync(client, unreadOnly: false);
        all.Items.Should().HaveCount(2);
        all.Items.Should().OnlyContain(n => n.IsRead);
    }

    [Fact]
    public async Task A_member_never_sees_another_members_notifications()
    {
        var client = _factory.CreateClient();
        var adminToken = await Api.AuthenticateAdminAsync(client);
        var prId = await Api.GetDepartmentIdAsync(client, "PR");

        const string assigneeEmail = "notif.only@ue-varna.bg";
        var assigneeId = await TaskApi.CreateMemberAsync(client, "Notif Only", assigneeEmail, "Member", prId);

        Api.UseBearer(client, adminToken);
        (await client.PostAsJsonAsync("/api/v1/tasks", new
        {
            title = "Solo task",
            description = (string?)null,
            priority = "Low",
            scope = "Departmental",
            departmentId = prId,
            dueAtUtc = "2026-12-01T10:00:00Z",
            assigneeIds = new[] { assigneeId }
        })).StatusCode.Should().Be(HttpStatusCode.Created);

        // The admin (creator) was not an assignee, so has no TaskAssigned notification of their own.
        var adminInbox = await GetAsync(client, unreadOnly: false);
        adminInbox.Items.Should().NotContain(n => n.Type == "TaskAssigned");
    }

    private static async Task<PagedNotificationsResponse> GetAsync(HttpClient client, bool unreadOnly)
    {
        var url = $"/api/v1/notifications?unreadOnly={unreadOnly.ToString().ToLowerInvariant()}";
        return (await client.GetFromJsonAsync<PagedNotificationsResponse>(url))!;
    }
}

public sealed record NotificationPayloadResponse(string Type, Guid? Id);

public sealed record NotificationResponse(
    Guid Id, string Type, string Title, string Body, NotificationPayloadResponse? Payload, bool IsRead, DateTime CreatedAtUtc);

public sealed record PagedNotificationsResponse(
    List<NotificationResponse> Items, int Page, int PageSize, int TotalCount, int TotalPages);
