using StudentCouncil.Application.Abstractions;

namespace StudentCouncil.Application.Features.Members;

/// <summary>Compact member view for lists, summaries and the auth <c>user</c> payload.</summary>
public sealed record MemberSummaryDto(
    Guid Id,
    string FullName,
    SystemRole Role,
    DepartmentCode? Department,
    MemberStatus Status,
    string? PhotoUrl);

/// <summary>Full member profile, including contact details.</summary>
public sealed record MemberDto(
    Guid Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    string? PhotoUrl,
    SystemRole Role,
    DepartmentCode? Department,
    string? DepartmentName,
    DateOnly JoinedOn,
    MemberStatus Status);

public static class MemberMappings
{
    public static MemberSummaryDto ToSummary(this MemberView view) =>
        new(view.Id, view.FullName, view.Role, view.DepartmentCode, view.Status, PhotoUrl(view));

    public static MemberSummaryDto ToSummary(this MemberAccount account) =>
        new(account.Id, account.FullName, account.Role, account.DepartmentCode, account.Status, null);

    public static MemberDto ToDto(this MemberView view) =>
        new(view.Id, view.FullName, view.Email, view.PhoneNumber, PhotoUrl(view),
            view.Role, view.DepartmentCode, view.DepartmentName, view.JoinedOn, view.Status);

    /// <summary>
    /// Members store an internal avatar storage key; clients receive the API endpoint that streams it
    /// (<c>GET /members/{id}/photo</c>). Null when the member has no photo.
    /// </summary>
    private static string? PhotoUrl(MemberView view) =>
        string.IsNullOrEmpty(view.PhotoUrl) ? null : $"/api/v1/members/{view.Id}/photo";
}
