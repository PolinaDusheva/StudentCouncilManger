using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace StudentCouncil.IntegrationTests;

// A fresh factory (database + rate-limit window) per test: each scenario re-seeds the admin and
// stays under the per-IP auth rate limit, matching the one-scenario-per-factory pattern.
public class TaskAuthorizationTests : IAsyncLifetime
{
    private readonly RecordingEmailFactory _factory = new();

    public Task InitializeAsync() => _factory.InitializeAsync();

    public Task DisposeAsync() => ((IAsyncLifetime)_factory).DisposeAsync();

    private static async Task<Guid> CreateDeptTaskAsync(HttpClient client, Guid departmentId, Guid assigneeId, string title)
    {
        var response = await client.PostAsJsonAsync("/api/v1/tasks", new
        {
            title,
            priority = "Medium",
            scope = "Departmental",
            departmentId,
            assigneeIds = new[] { assigneeId }
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<TaskDetailResponse>())!.Id;
    }

    [Fact]
    public async Task Member_cannot_see_others_tasks_or_create_one()
    {
        var client = _factory.CreateClient();
        var adminToken = await Api.AuthenticateAdminAsync(client);
        var prId = await Api.GetDepartmentIdAsync(client, "PR");

        var insiderId = await TaskApi.CreateMemberAsync(client, "Insider", "vis.insider@ue-varna.bg", "Member", prId);
        await TaskApi.CreateMemberAsync(client, "Outsider", "vis.outsider@ue-varna.bg", "Member", prId);

        Api.UseBearer(client, adminToken);
        var taskId = await CreateDeptTaskAsync(client, prId, insiderId, "Insider only task");

        // The outsider can neither read nor list the task, and may not create tasks.
        await TaskApi.SignInFreshMemberAsync(client, _factory.Email, "vis.outsider@ue-varna.bg", "OutsiderPass1");

        (await client.GetAsync($"/api/v1/tasks/{taskId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var list = await client.GetFromJsonAsync<PagedTasksResponse>("/api/v1/tasks");
        list!.Items.Should().NotContain(t => t.Id == taskId);

        var mine = await client.GetFromJsonAsync<List<TaskListItemResponse>>("/api/v1/tasks/mine");
        mine!.Should().BeEmpty();

        var create = await client.PostAsJsonAsync("/api/v1/tasks", new
        {
            title = "Member tries to create",
            priority = "Low",
            scope = "Departmental",
            departmentId = prId,
            assigneeIds = new[] { insiderId }
        });
        create.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Dept_leadership_edit_is_scoped_to_their_department()
    {
        var client = _factory.CreateClient();
        var adminToken = await Api.AuthenticateAdminAsync(client);
        var prId = await Api.GetDepartmentIdAsync(client, "PR");
        var sportsId = await Api.GetDepartmentIdAsync(client, "Sports");

        var prVpId = await TaskApi.CreateMemberAsync(client, "PR VP", "edit.prvp@ue-varna.bg", "DeptVicePresident", prId);
        var sportsMemberId = await TaskApi.CreateMemberAsync(client, "Sports Member", "edit.sports@ue-varna.bg", "Member", sportsId);

        Api.UseBearer(client, adminToken);
        var sportsTaskId = await CreateDeptTaskAsync(client, sportsId, sportsMemberId, "Sports task");

        // An organisational task assigned to the PR VP is visible to them but not editable (org-only).
        var orgTaskResponse = await client.PostAsJsonAsync("/api/v1/tasks", new
        {
            title = "Org initiative",
            priority = "Medium",
            scope = "Organizational",
            departmentId = (Guid?)null,
            assigneeIds = new[] { prVpId }
        });
        orgTaskResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var orgTaskId = (await orgTaskResponse.Content.ReadFromJsonAsync<TaskDetailResponse>())!.Id;

        await TaskApi.SignInFreshMemberAsync(client, _factory.Email, "edit.prvp@ue-varna.bg", "PrVpPass1");

        var editBody = new { title = "Hijacked title", description = (string?)null, priority = "High", dueAtUtc = (string?)null, assigneeIds = new[] { sportsMemberId } };

        // Cross-department task is invisible -> 404 (existence not leaked).
        (await client.PutAsJsonAsync($"/api/v1/tasks/{sportsTaskId}", editBody))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Visible-but-not-editable organisational task -> 403.
        (await client.PutAsJsonAsync($"/api/v1/tasks/{orgTaskId}",
                new { title = "Edit attempt", description = (string?)null, priority = "High", dueAtUtc = (string?)null, assigneeIds = new[] { prVpId } }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Only_org_president_can_delete_a_task()
    {
        var client = _factory.CreateClient();
        var adminToken = await Api.AuthenticateAdminAsync(client);
        var prId = await Api.GetDepartmentIdAsync(client, "PR");

        var presId = await TaskApi.CreateMemberAsync(client, "PR President", "del.prpres@ue-varna.bg", "DeptPresident", prId);
        var memberId = await TaskApi.CreateMemberAsync(client, "PR Member", "del.member@ue-varna.bg", "Member", prId);

        Api.UseBearer(client, adminToken);
        var taskId = await CreateDeptTaskAsync(client, prId, memberId, "Deletable task");

        // Dept president (even of the owning department) cannot delete.
        await TaskApi.SignInFreshMemberAsync(client, _factory.Email, "del.prpres@ue-varna.bg", "PrPresPass1");
        (await client.DeleteAsync($"/api/v1/tasks/{taskId}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // OrgPresident can.
        Api.UseBearer(client, adminToken);
        (await client.DeleteAsync($"/api/v1/tasks/{taskId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetAsync($"/api/v1/tasks/{taskId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Document_deletion_is_restricted_to_uploader_or_org_leadership()
    {
        var client = _factory.CreateClient();
        var adminToken = await Api.AuthenticateAdminAsync(client);
        var prId = await Api.GetDepartmentIdAsync(client, "PR");

        var uploaderId = await TaskApi.CreateMemberAsync(client, "Uploader", "doc.uploader@ue-varna.bg", "Member", prId);
        var otherId = await TaskApi.CreateMemberAsync(client, "Other", "doc.other@ue-varna.bg", "Member", prId);

        Api.UseBearer(client, adminToken);
        var taskId = await CreateDeptTaskAsync(client, prId, uploaderId, "Task with documents");
        // Add the second member so they can see the task but are not the uploader.
        (await client.PutAsJsonAsync($"/api/v1/tasks/{taskId}",
            new { title = "Task with documents", description = (string?)null, priority = "Medium", dueAtUtc = (string?)null, assigneeIds = new[] { uploaderId, otherId } }))
            .EnsureSuccessStatusCode();

        // Uploader attaches a document.
        await TaskApi.SignInFreshMemberAsync(client, _factory.Email, "doc.uploader@ue-varna.bg", "UploaderPass1");
        var upload = await client.PostAsync($"/api/v1/tasks/{taskId}/documents", TaskApi.PdfUpload());
        upload.StatusCode.Should().Be(HttpStatusCode.Created);
        var documentId = (await upload.Content.ReadFromJsonAsync<TaskDocumentResponse>())!.Id;

        // A different member who can see the task still cannot delete someone else's document.
        await TaskApi.SignInFreshMemberAsync(client, _factory.Email, "doc.other@ue-varna.bg", "OtherPass1");
        (await client.DeleteAsync($"/api/v1/tasks/{taskId}/documents/{documentId}"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Org leadership can.
        Api.UseBearer(client, adminToken);
        (await client.DeleteAsync($"/api/v1/tasks/{taskId}/documents/{documentId}"))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Upload_rejects_a_file_whose_content_does_not_match_its_extension()
    {
        var client = _factory.CreateClient();
        var adminToken = await Api.AuthenticateAdminAsync(client);
        var prId = await Api.GetDepartmentIdAsync(client, "PR");

        var memberId = await TaskApi.CreateMemberAsync(client, "Bad Upload", "doc.bad@ue-varna.bg", "Member", prId);

        Api.UseBearer(client, adminToken);
        var taskId = await CreateDeptTaskAsync(client, prId, memberId, "Bad upload task");

        await TaskApi.SignInFreshMemberAsync(client, _factory.Email, "doc.bad@ue-varna.bg", "BadUploadPass1");
        var upload = await client.PostAsync($"/api/v1/tasks/{taskId}/documents", TaskApi.FakePdfUpload());

        upload.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Api.ReadCodeAsync(upload)).Should().Be("unsupported_file_type");
    }

    [Fact]
    public async Task Create_enforces_the_scope_rule_for_dept_leadership()
    {
        var client = _factory.CreateClient();
        var adminToken = await Api.AuthenticateAdminAsync(client);
        var prId = await Api.GetDepartmentIdAsync(client, "PR");
        var sportsId = await Api.GetDepartmentIdAsync(client, "Sports");

        var prPresId = await TaskApi.CreateMemberAsync(client, "PR President", "scope.prpres@ue-varna.bg", "DeptPresident", prId);
        var prMemberId = await TaskApi.CreateMemberAsync(client, "PR Member", "scope.member@ue-varna.bg", "Member", prId);

        await TaskApi.SignInFreshMemberAsync(client, _factory.Email, "scope.prpres@ue-varna.bg", "ScopePrPres1");

        // Organisational scope requires org leadership.
        (await client.PostAsJsonAsync("/api/v1/tasks", new
        {
            title = "Dept tries org task",
            priority = "Medium",
            scope = "Organizational",
            departmentId = (Guid?)null,
            assigneeIds = new[] { prMemberId }
        })).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Departmental task for a different department is forbidden.
        (await client.PostAsJsonAsync("/api/v1/tasks", new
        {
            title = "Dept tries other dept",
            priority = "Medium",
            scope = "Departmental",
            departmentId = sportsId,
            assigneeIds = new[] { prMemberId }
        })).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Departmental task for their own department succeeds.
        (await client.PostAsJsonAsync("/api/v1/tasks", new
        {
            title = "Dept own task",
            priority = "Medium",
            scope = "Departmental",
            departmentId = prId,
            assigneeIds = new[] { prMemberId }
        })).StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
