using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentCouncil.Application.Common.Audit;
using StudentCouncil.Domain.Entities;
using StudentCouncil.Infrastructure.Persistence;

namespace StudentCouncil.IntegrationTests;

// Fresh factory (database + rate-limit window) per test, matching the one-scenario-per-factory pattern.
// Each sensitive action (spec 14) must leave exactly one AuditLog row attributed to the acting admin.
public class AuditTrailTests : IAsyncLifetime
{
    private readonly RecordingEmailFactory _factory = new();

    public Task InitializeAsync() => _factory.InitializeAsync();

    public Task DisposeAsync() => ((IAsyncLifetime)_factory).DisposeAsync();

    [Fact]
    public async Task Member_lifecycle_is_audited_with_role_change_distinguished()
    {
        var client = _factory.CreateClient();
        await Api.AuthenticateAdminAsync(client);
        var adminId = await AdminIdAsync();
        var prId = await Api.GetDepartmentIdAsync(client, "PR");

        var memberId = await TaskApi.CreateMemberAsync(client, "Audit Target", "audit.target@ue-varna.bg", "Member", prId);

        var afterCreate = await AuditRowsAsync(AuditEntities.Member, memberId);
        var createdRow = afterCreate.Should().ContainSingle(a => a.Action == AuditActions.MemberCreated).Subject;
        createdRow.ActorId.Should().Be(adminId);
        createdRow.Details.Should().Contain("audit.target@ue-varna.bg");

        // A role change is recorded as RoleChanged (spec 14 names role changes specifically).
        (await UpdateMemberAsync(client, memberId, "Audit Target", "DeptSecretary", prId))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await AuditRowsAsync(AuditEntities.Member, memberId))
            .Should().Contain(a => a.Action == AuditActions.RoleChanged);

        // A plain field edit (no role/department change) is recorded as MemberUpdated.
        (await UpdateMemberAsync(client, memberId, "Audit Target Renamed", "DeptSecretary", prId))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await AuditRowsAsync(AuditEntities.Member, memberId))
            .Should().Contain(a => a.Action == AuditActions.MemberUpdated);

        (await client.PostAsync($"/api/v1/members/{memberId}/deactivate", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.PostAsync($"/api/v1/members/{memberId}/reactivate", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var all = await AuditRowsAsync(AuditEntities.Member, memberId);
        all.Select(a => a.Action).Should().Contain([
            AuditActions.MemberCreated, AuditActions.RoleChanged, AuditActions.MemberUpdated,
            AuditActions.MemberDeactivated, AuditActions.MemberReactivated
        ]);
        all.Should().OnlyContain(a => a.ActorId == adminId);
    }

    [Fact]
    public async Task Expense_mutations_are_audited_and_the_update_records_only_changed_fields()
    {
        var client = _factory.CreateClient();
        await Api.AuthenticateAdminAsync(client);
        var adminId = await AdminIdAsync();

        var create = await client.PostAsJsonAsync("/api/v1/budget/expenses",
            new { description = "Audit expense", amountEur = 50m, spentOn = "2026-06-01" });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var expense = (await create.Content.ReadFromJsonAsync<ExpenseResponse>())!;

        // Change amount + description but keep the spend date — the diff must omit the unchanged field.
        (await client.PutAsJsonAsync($"/api/v1/budget/expenses/{expense.Id}",
            new { description = "Audit expense edited", amountEur = 75m, spentOn = "2026-06-01" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await client.DeleteAsync($"/api/v1/budget/expenses/{expense.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var rows = await AuditRowsAsync(AuditEntities.Expense, expense.Id);
        rows.Select(a => a.Action).Should().BeEquivalentTo([
            AuditActions.ExpenseAdded, AuditActions.ExpenseUpdated, AuditActions.ExpenseDeleted
        ]);
        rows.Should().OnlyContain(a => a.ActorId == adminId);

        var updateDetails = rows.Single(a => a.Action == AuditActions.ExpenseUpdated).Details!;
        updateDetails.Should().Contain("amountEur").And.Contain("description");
        updateDetails.Should().NotContain("spentOn");
    }

    [Fact]
    public async Task Duty_mutations_are_audited()
    {
        var client = _factory.CreateClient();
        var adminToken = await Api.AuthenticateAdminAsync(client);
        var adminId = await AdminIdAsync();
        var prId = await Api.GetDepartmentIdAsync(client, "PR");

        var memberId = await TaskApi.CreateMemberAsync(client, "Duty Audit", "duty.audit@ue-varna.bg", "Member", prId);

        Api.UseBearer(client, adminToken);
        var create = await client.PostAsJsonAsync("/api/v1/duty-records",
            new { memberId, startUtc = "2026-06-10T09:00:00Z", endUtc = "2026-06-10T11:00:00Z", note = (string?)null });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var duty = (await create.Content.ReadFromJsonAsync<DutyRecordResponse>())!;

        (await client.PutAsJsonAsync($"/api/v1/duty-records/{duty.Id}",
            new { startUtc = "2026-06-10T09:00:00Z", endUtc = "2026-06-10T12:00:00Z", note = "extended" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await client.DeleteAsync($"/api/v1/duty-records/{duty.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var rows = await AuditRowsAsync(AuditEntities.DutyRecord, duty.Id);
        rows.Select(a => a.Action).Should().BeEquivalentTo([
            AuditActions.DutyRegistered, AuditActions.DutyUpdated, AuditActions.DutyDeleted
        ]);
        rows.Should().OnlyContain(a => a.ActorId == adminId);
    }

    [Fact]
    public async Task Task_and_document_deletions_are_audited()
    {
        var client = _factory.CreateClient();
        var adminToken = await Api.AuthenticateAdminAsync(client);
        var adminId = await AdminIdAsync();
        var prId = await Api.GetDepartmentIdAsync(client, "PR");

        var assigneeId = await TaskApi.CreateMemberAsync(client, "Task Audit", "task.audit@ue-varna.bg", "Member", prId);

        Api.UseBearer(client, adminToken);
        var createTask = await client.PostAsJsonAsync("/api/v1/tasks", new
        {
            title = "Doomed task",
            description = (string?)null,
            priority = "Low",
            scope = "Departmental",
            departmentId = prId,
            dueAtUtc = "2026-12-01T10:00:00Z",
            assigneeIds = new[] { assigneeId }
        });
        createTask.StatusCode.Should().Be(HttpStatusCode.Created);
        var task = (await createTask.Content.ReadFromJsonAsync<TaskDetailResponse>())!;

        var upload = await client.PostAsync($"/api/v1/tasks/{task.Id}/documents", TaskApi.PdfUpload());
        upload.StatusCode.Should().Be(HttpStatusCode.Created);
        var document = (await upload.Content.ReadFromJsonAsync<TaskDocumentResponse>())!;

        // Delete the document explicitly (its own audit) before deleting the task.
        (await client.DeleteAsync($"/api/v1/tasks/{task.Id}/documents/{document.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var docRows = await AuditRowsAsync(AuditEntities.TaskDocument, document.Id);
        docRows.Should().ContainSingle(a => a.Action == AuditActions.DocumentDeleted);
        docRows.Single().ActorId.Should().Be(adminId);

        (await client.DeleteAsync($"/api/v1/tasks/{task.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var taskRows = await AuditRowsAsync(AuditEntities.TaskItem, task.Id);
        var deleted = taskRows.Should().ContainSingle(a => a.Action == AuditActions.TaskDeleted).Subject;
        deleted.ActorId.Should().Be(adminId);
        deleted.Details.Should().Contain("Doomed task");
    }

    [Fact]
    public async Task A_failed_action_writes_no_audit_row()
    {
        var client = _factory.CreateClient();
        await Api.AuthenticateAdminAsync(client);

        // Deleting a non-existent expense fails before the recorder is reached — nothing is staged or committed.
        var ghost = Guid.NewGuid();
        (await client.DeleteAsync($"/api/v1/budget/expenses/{ghost}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await AuditRowsAsync(AuditEntities.Expense, ghost)).Should().BeEmpty();
    }

    private static Task<HttpResponseMessage> UpdateMemberAsync(
        HttpClient client, Guid memberId, string fullName, string role, Guid departmentId) =>
        client.PutAsJsonAsync($"/api/v1/members/{memberId}", new
        {
            fullName,
            phoneNumber = (string?)null,
            role,
            departmentId,
            joinedOn = "2026-02-01"
        });

    private async Task<List<AuditLog>> AuditRowsAsync(string entityType, Guid entityId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var id = entityId.ToString();
        return await db.AuditLogs
            .AsNoTracking()
            .Where(a => a.EntityType == entityType && a.EntityId == id)
            .ToListAsync();
    }

    private async Task<Guid> AdminIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Users
            .Where(u => u.Email == SmokeTestFactory.AdminEmail)
            .Select(u => u.Id)
            .SingleAsync();
    }
}
