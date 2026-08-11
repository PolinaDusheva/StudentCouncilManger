namespace StudentCouncil.Application.Abstractions;

/// <summary>The raw (un-hashed) refresh token plus its expiry. Returned only at issue/rotation time.</summary>
public sealed record RefreshTokenResult(string RawToken, DateTime ExpiresAtUtc);

/// <summary>A rotated token, carrying the owning member id so the caller can re-check status.</summary>
public sealed record RotatedToken(string RawToken, DateTime ExpiresAtUtc, Guid MemberId);

public interface IRefreshTokenService
{
    /// <summary>Issues a new refresh token for the member, storing only its hash.</summary>
    Task<RefreshTokenResult> IssueAsync(Guid memberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and rotates a refresh token: revokes the presented token and issues a new one.
    /// Reuse of an already-rotated token revokes the whole chain.
    /// </summary>
    Task<RotatedToken> RotateAsync(string rawToken, CancellationToken cancellationToken = default);

    /// <summary>Revokes the presented token (logout).</summary>
    Task RevokeAsync(string rawToken, CancellationToken cancellationToken = default);

    /// <summary>Revokes every active token for a member (deactivation / password change).</summary>
    Task RevokeAllForMemberAsync(Guid memberId, CancellationToken cancellationToken = default);
}
