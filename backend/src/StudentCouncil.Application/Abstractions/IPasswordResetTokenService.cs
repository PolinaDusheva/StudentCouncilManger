namespace StudentCouncil.Application.Abstractions;

/// <summary>Issues and validates short-lived password reset tokens. Implementation in Phase 2.</summary>
public interface IPasswordResetTokenService
{
    Task<string> GenerateAsync(Guid memberId, CancellationToken cancellationToken = default);

    /// <summary>Returns the member id if the token is valid and unexpired, otherwise <c>null</c>.</summary>
    Task<Guid?> ValidateAsync(string token, CancellationToken cancellationToken = default);
}
