using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace StudentCouncil.IntegrationTests;

// Response shapes (enums are serialised as strings by the API).
public sealed record TokensDto(
    string AccessToken, string RefreshToken, int ExpiresInSeconds, bool MustChangePassword, UserSummaryDto User);

public sealed record UserSummaryDto(Guid Id, string FullName, string Role, string? Department, string Status);

public sealed record MeDto(
    Guid Id, string FullName, string Email, string? PhoneNumber, string Role,
    string? Department, string? DepartmentName, string Status, PermissionsDto Permissions);

public sealed record PermissionsDto(
    bool CanManageMembers, bool CanManageBudget, bool CanManageDuties, bool CanCreateOrgTask,
    bool CanCreateDeptTask, bool CanManageEvents);

public sealed record MemberResponse(
    Guid Id, string FullName, string Email, string? PhoneNumber, string? PhotoUrl,
    string Role, string? Department, string? DepartmentName, DateOnly JoinedOn, string Status);

public sealed record DepartmentResponse(
    Guid Id, string Code, string Name, string? Description, int MemberCount, LeadershipResponse Leadership);

public sealed record LeadershipResponse(UserSummaryDto? President, UserSummaryDto? VicePresident, UserSummaryDto? Secretary);

/// <summary>HTTP helpers shared by the integration tests.</summary>
public static class Api
{
    public static Task<HttpResponseMessage> LoginRawAsync(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });

    public static async Task<TokensDto> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await LoginRawAsync(client, email, password);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<TokensDto>())!;
    }

    public static void UseBearer(HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    public static void ClearBearer(HttpClient client) =>
        client.DefaultRequestHeaders.Authorization = null;

    /// <summary>Logs in the seeded admin, changes the temporary password and returns a usable access token.</summary>
    public static async Task<string> AuthenticateAdminAsync(HttpClient client, string newPassword = "ChangedAdmin1")
    {
        var initial = await LoginAsync(client, SmokeTestFactory.AdminEmail, SmokeTestFactory.AdminInitialPassword);
        UseBearer(client, initial.AccessToken);

        var change = await client.PostAsJsonAsync("/api/v1/auth/change-password",
            new { currentPassword = SmokeTestFactory.AdminInitialPassword, newPassword });
        change.StatusCode.Should().Be(HttpStatusCode.NoContent);

        ClearBearer(client);
        var loggedIn = await LoginAsync(client, SmokeTestFactory.AdminEmail, newPassword);
        UseBearer(client, loggedIn.AccessToken);
        return loggedIn.AccessToken;
    }

    public static async Task<Guid> GetDepartmentIdAsync(HttpClient client, string code)
    {
        var departments = await client.GetFromJsonAsync<List<DepartmentResponse>>("/api/v1/departments");
        return departments!.Single(d => d.Code == code).Id;
    }

    public static string ExtractTemporaryPassword(RecordingEmailSender recorder, string email)
    {
        var message = recorder.Messages.Last(m => m.To == email);
        var match = Regex.Match(message.Body, @"Временна парола:</strong>\s*([^<]+?)</p>");
        match.Success.Should().BeTrue("the welcome email should contain a temporary password");
        return match.Groups[1].Value.Trim();
    }

    public static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}
