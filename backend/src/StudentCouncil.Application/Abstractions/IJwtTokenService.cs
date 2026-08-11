namespace StudentCouncil.Application.Abstractions;

/// <summary>
/// Identity-free projection of the user needed to mint an access token.
/// Keeps the Identity <c>ApplicationUser</c> type out of the Application layer.
/// </summary>
public sealed record TokenUser(
    Guid Id,
    string Email,
    string FullName,
    SystemRole Role,
    Guid? DepartmentId,
    DepartmentCode? Department,
    string SecurityStamp,
    bool MustChangePassword);

public sealed record AccessToken(string Token, DateTime ExpiresAtUtc, int ExpiresInSeconds);

public interface IJwtTokenService
{
    AccessToken CreateAccessToken(TokenUser user);
}
