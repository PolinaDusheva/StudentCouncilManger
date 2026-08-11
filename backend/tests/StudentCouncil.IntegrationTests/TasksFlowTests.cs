using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace StudentCouncil.IntegrationTests;

public class TasksFlowTests : IClassFixture<RecordingEmailFactory>
{
    private readonly RecordingEmailFactory _factory;

    public TasksFlowTests(RecordingEmailFactory factory) => _factory = factory;

    [Fact]
    public async Task Create_assign_transition_comment_and_attach_a_document_end_to_end()
    {
        var client = _factory.CreateClient();
        var adminToken = await Api.AuthenticateAdminAsync(client);
        var socialId = await Api.GetDepartmentIdAsync(client, "Social");

        const string assigneeEmail = "flow.assignee@ue-varna.bg";
        var assigneeId = await TaskApi.CreateMemberAsync(client, "Flow Assignee", assigneeEmail, "Member", socialId);

        // OrgPresident creates a departmental task for the member.
        Api.UseBearer(client, adminToken);
        var createResponse = await client.PostAsJsonAsync("/api/v1/tasks", new
        {
            title = "Organise welcome day",
            description = "Plan the welcome event",
            priority = "High",
            scope = "Departmental",
            departmentId = socialId,
            dueAtUtc = "2026-12-01T10:00:00Z",
            assigneeIds = new[] { assigneeId }
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var task = (await createResponse.Content.ReadFromJsonAsync<TaskDetailResponse>())!;
        task.Status.Should().Be("New");
        task.Department.Should().Be("Social");
        task.Assignees.Should().ContainSingle(a => a.Id == assigneeId);

        // The assignee signs in and sees the task in /mine and on the board.
        await TaskApi.SignInFreshMemberAsync(client, _factory.Email, assigneeEmail, "AssigneePass1");
        var mine = await client.GetFromJsonAsync<List<TaskListItemResponse>>("/api/v1/tasks/mine");
        mine!.Should().ContainSingle(t => t.Id == task.Id);

        var board = await client.GetFromJsonAsync<TaskBoardResponse>("/api/v1/tasks/board");
        board!.Columns.New.Should().Contain(t => t.Id == task.Id);

        // Assignee self-service: New -> InProgress -> InReview.
        (await client.PatchAsJsonAsync($"/api/v1/tasks/{task.Id}/status", new { status = "InProgress" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PatchAsJsonAsync($"/api/v1/tasks/{task.Id}/status", new { status = "InReview" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // The assignee cannot complete it themselves (not a leadership transition).
        (await client.PatchAsJsonAsync($"/api/v1/tasks/{task.Id}/status", new { status = "Completed" }))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Comment + document upload/download round-trip.
        (await client.PostAsJsonAsync($"/api/v1/tasks/{task.Id}/comments", new { text = "Started working on it." }))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var upload = await client.PostAsync($"/api/v1/tasks/{task.Id}/documents", TaskApi.PdfUpload());
        upload.StatusCode.Should().Be(HttpStatusCode.Created);
        var document = (await upload.Content.ReadFromJsonAsync<TaskDocumentResponse>())!;
        document.OriginalFileName.Should().Be("sample.pdf");

        var download = await client.GetAsync($"/api/v1/tasks/{task.Id}/documents/{document.Id}/download");
        download.StatusCode.Should().Be(HttpStatusCode.OK);
        var bytes = await download.Content.ReadAsByteArrayAsync();
        bytes.Take(4).Should().Equal("%PDF"u8.ToArray());

        // Leadership completes the task; the detail view reflects the comment and document.
        Api.UseBearer(client, adminToken);
        var complete = await client.PatchAsJsonAsync($"/api/v1/tasks/{task.Id}/status", new { status = "Completed" });
        complete.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await client.GetFromJsonAsync<TaskDetailResponse>($"/api/v1/tasks/{task.Id}");
        detail!.Status.Should().Be("Completed");
        detail.Comments.Should().ContainSingle(c => c.Text == "Started working on it.");
        detail.Documents.Should().ContainSingle(d => d.Id == document.Id);

        var board2 = await client.GetFromJsonAsync<TaskBoardResponse>("/api/v1/tasks/board");
        board2!.Columns.Completed.Should().Contain(t => t.Id == task.Id);
    }
}
