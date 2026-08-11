using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Features.Members;

namespace StudentCouncil.Application.Features.Auth;

/// <summary>Access + refresh token pair returned by login and refresh.</summary>
public sealed record AuthTokensResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds,
    bool MustChangePassword,
    MemberSummaryDto User);

internal static class AuthMapping
{
    public static TokenUser ToTokenUser(this MemberAccount account) =>
        new(account.Id, account.Email, account.FullName, account.Role,
            account.DepartmentId, account.DepartmentCode, account.SecurityStamp, account.MustChangePassword);
}
