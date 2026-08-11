using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Domain.Entities;
using StudentCouncil.Domain.Enums;
using StudentCouncil.Infrastructure.BackgroundJobs;

namespace StudentCouncil.IntegrationTests;

// Background-job timers are disabled in the test host (SmokeTestFactory), so each tick is driven
// explicitly against a fresh scope for a deterministic assertion.
public class BackgroundJobsTests : IAsyncLifetime
{
    private readonly RecordingNotificationsFactory _factory = new();

    public Task InitializeAsync() => _factory.InitializeAsync();

    public Task DisposeAsync() => ((IAsyncLifetime)_factory).DisposeAsync();

    [Fact]
    public async Task Task_due_soon_tick_notifies_assignees_once()
    {
        var client = _factory.CreateClient();
        var adminToken = await Api.AuthenticateAdminAsync(client);
        var prId = await Api.GetDepartmentIdAsync(client, "PR");

        var assigneeId = await TaskApi.CreateMemberAsync(client, "Due Soon", "due.soon@ue-varna.bg", "Member", prId);

        Api.UseBearer(client, adminToken);
        var dueAt = DateTime.UtcNow.AddHours(12).ToString("yyyy-MM-ddTHH:mm:ssZ");
        var create = await client.PostAsJsonAsync("/api/v1/tasks", new
        {
            title = "Due within a day",
            description = (string?)null,
            priority = "High",
            scope = "Departmental",
            departmentId = prId,
            dueAtUtc = dueAt,
            assigneeIds = new[] { assigneeId }
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        await RunTickAsync<TaskDueSoonJob>();

        // The assignee was reminded, and an in-app row was written.
        DueSoonPushCount(assigneeId).Should().Be(1);
        (await DueSoonRowCountAsync(assigneeId)).Should().Be(1);

        // The idempotency marker means a second tick does not notify again.
        await RunTickAsync<TaskDueSoonJob>();
        DueSoonPushCount(assigneeId).Should().Be(1);
        (await DueSoonRowCountAsync(assigneeId)).Should().Be(1);
    }

    [Fact]
    public async Task Refresh_token_cleanup_removes_expired_and_revoked_tokens_only()
    {
        var client = _factory.CreateClient();
        await Api.AuthenticateAdminAsync(client);

        var now = DateTime.UtcNow;
        await SeedRefreshTokensAsync(
            new RefreshToken { Id = Guid.NewGuid(), MemberId = Guid.NewGuid(), TokenHash = "expired-hash", CreatedAtUtc = now.AddDays(-31), ExpiresAtUtc = now.AddDays(-1) },
            new RefreshToken { Id = Guid.NewGuid(), MemberId = Guid.NewGuid(), TokenHash = "revoked-hash", CreatedAtUtc = now.AddDays(-2), ExpiresAtUtc = now.AddDays(28), RevokedAtUtc = now.AddDays(-1) },
            new RefreshToken { Id = Guid.NewGuid(), MemberId = Guid.NewGuid(), TokenHash = "active-hash", CreatedAtUtc = now, ExpiresAtUtc = now.AddDays(30) });

        await RunTickAsync<RefreshTokenCleanupJob>();

        var remaining = await RefreshTokenHashesAsync();
        remaining.Should().Contain("active-hash");
        remaining.Should().NotContain(["expired-hash", "revoked-hash"]);
    }

    [Fact]
    public async Task Orphan_file_cleanup_deletes_only_unreferenced_blobs()
    {
        var client = _factory.CreateClient();
        var adminToken = await Api.AuthenticateAdminAsync(client);
        var prId = await Api.GetDepartmentIdAsync(client, "PR");
        var assigneeId = await TaskApi.CreateMemberAsync(client, "Orphan", "orphan.assignee@ue-varna.bg", "Member", prId);

        // A real task with an uploaded document gives us a referenced blob.
        Api.UseBearer(client, adminToken);
        var create = await client.PostAsJsonAsync("/api/v1/tasks", new
        {
            title = "Has a document",
            description = (string?)null,
            priority = "Low",
            scope = "Departmental",
            departmentId = prId,
            dueAtUtc = "2026-12-01T10:00:00Z",
            assigneeIds = new[] { assigneeId }
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var task = (await create.Content.ReadFromJsonAsync<TaskDetailResponse>())!;
        (await client.PostAsync($"/api/v1/tasks/{task.Id}/documents", TaskApi.PdfUpload()))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        // Seed an orphan blob (no TaskDocument row) directly into storage and capture the referenced key.
        var orphanKey = $"tasks/{Guid.NewGuid():N}/orphan.bin";
        string referencedKey;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            referencedKey = await db.TaskDocuments.Select(d => d.StorageKey).SingleAsync();

            var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
            await using var content = new MemoryStream([1, 2, 3]);
            await storage.SaveAsync(content, orphanKey, "application/octet-stream");
        }

        await RunTickAsync<OrphanFileCleanupJob>();

        var keys = await StorageKeysAsync("tasks/");
        keys.Should().Contain(referencedKey);
        keys.Should().NotContain(orphanKey);

        // Idempotent: the job_runs marker means a second tick changes nothing.
        await RunTickAsync<OrphanFileCleanupJob>();
        (await StorageKeysAsync("tasks/")).Should().Contain(referencedKey);
    }

    private async Task<List<string>> StorageKeysAsync(string prefix)
    {
        using var scope = _factory.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

        var keys = new List<string>();
        await foreach (var key in storage.EnumerateKeysAsync(prefix))
        {
            keys.Add(key);
        }

        return keys;
    }

    private async Task RunTickAsync<TJob>() where TJob : PeriodicBackgroundService
    {
        using var scope = _factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<TJob>();
        await job.ExecuteTickAsync(scope.ServiceProvider, default);
    }

    private int DueSoonPushCount(Guid assigneeId) =>
        _factory.Push.Sent.Count(p => p.Type == NotificationType.TaskDueSoon && p.MemberIds.Contains(assigneeId));

    private async Task<int> DueSoonRowCountAsync(Guid recipientId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        return await db.Notifications.CountAsync(n =>
            n.RecipientId == recipientId && n.Type == NotificationType.TaskDueSoon);
    }

    private async Task SeedRefreshTokensAsync(params RefreshToken[] tokens)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        db.RefreshTokens.AddRange(tokens);
        await db.SaveChangesAsync();
    }

    private async Task<List<string>> RefreshTokenHashesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        return await db.RefreshTokens.Select(t => t.TokenHash).ToListAsync();
    }
}
