using System.Net.Http.Json;
using FluentAssertions;

namespace StudentCouncil.IntegrationTests;

// Response shapes for the tasks/documents endpoints (enums serialised as strings).
public sealed record MemberSummaryResponse(
    Guid Id, string FullName, string Role, string? Department, string Status, string? PhotoUrl);

public sealed record TaskListItemResponse(
    Guid Id, string Title, string Priority, string Status, DateTime? DueAtUtc, string Scope,
    string? Department, int AssigneeCount, int CommentCount, int DocumentCount, bool IsOverdue);

public sealed record PagedTasksResponse(
    List<TaskListItemResponse> Items, int Page, int PageSize, int TotalCount, int TotalPages);

public sealed record TaskCommentResponse(Guid Id, MemberSummaryResponse? Author, string Text, DateTime CreatedAtUtc);

public sealed record TaskDocumentResponse(
    Guid Id, string OriginalFileName, string ContentType, long SizeBytes,
    MemberSummaryResponse? UploadedBy, DateTime UploadedAtUtc);

public sealed record TaskDetailResponse(
    Guid Id, string Title, string? Description, string Priority, string Status, DateTime? DueAtUtc, string Scope,
    string? Department, MemberSummaryResponse? CreatedBy, DateTime CreatedAtUtc,
    List<MemberSummaryResponse> Assignees, List<TaskDocumentResponse> Documents, List<TaskCommentResponse> Comments);

public sealed record TaskBoardResponse(BoardColumnsResponse Columns);

public sealed record BoardColumnsResponse(
    List<TaskListItemResponse> New, List<TaskListItemResponse> InProgress,
    List<TaskListItemResponse> InReview, List<TaskListItemResponse> Completed);

/// <summary>Helpers for the tasks/documents integration tests.</summary>
public static class TaskApi
{
    /// <summary>Creates a member (as the currently bearer-authenticated admin) and returns its id.</summary>
    public static async Task<Guid> CreateMemberAsync(
        HttpClient client, string fullName, string email, string role, Guid departmentId)
    {
        var create = await client.PostAsJsonAsync("/api/v1/members", new
        {
            fullName,
            email,
            phoneNumber = (string?)null,
            role,
            departmentId,
            joinedOn = "2026-02-01"
        });
        create.IsSuccessStatusCode.Should().BeTrue($"creating {email} should succeed");
        return (await create.Content.ReadFromJsonAsync<MemberResponse>())!.Id;
    }

    /// <summary>Clears a member's password gate and returns a usable access token (leaves bearer set to it).</summary>
    public static async Task<string> SignInFreshMemberAsync(
        HttpClient client, RecordingEmailSender email, string memberEmail, string newPassword)
    {
        var temporaryPassword = Api.ExtractTemporaryPassword(email, memberEmail);
        Api.ClearBearer(client);
        var firstLogin = await Api.LoginAsync(client, memberEmail, temporaryPassword);
        Api.UseBearer(client, firstLogin.AccessToken);

        var change = await client.PostAsJsonAsync("/api/v1/auth/change-password",
            new { currentPassword = temporaryPassword, newPassword });
        change.EnsureSuccessStatusCode();

        Api.ClearBearer(client);
        var session = await Api.LoginAsync(client, memberEmail, newPassword);
        Api.UseBearer(client, session.AccessToken);
        return session.AccessToken;
    }

    /// <summary>A small, valid PDF as multipart form content under the field name "file".</summary>
    public static MultipartFormDataContent PdfUpload(string fileName = "sample.pdf")
    {
        var bytes = "%PDF-1.7\n1 0 obj<<>>endobj\ntrailer<<>>\n%%EOF"u8.ToArray();
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");

        var form = new MultipartFormDataContent();
        form.Add(content, "file", fileName);
        return form;
    }

    public static MultipartFormDataContent FakePdfUpload(string fileName = "malware.pdf")
    {
        var bytes = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00 }; // "MZ" — a Windows executable
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");

        var form = new MultipartFormDataContent();
        form.Add(content, "file", fileName);
        return form;
    }
}
