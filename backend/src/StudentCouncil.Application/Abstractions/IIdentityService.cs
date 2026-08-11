using StudentCouncil.Application.Common.Models;

namespace StudentCouncil.Application.Abstractions;

/// <summary>Identity-free snapshot of a member, enough to authenticate and mint tokens.</summary>
public sealed record MemberAccount(
    Guid Id,
    string Email,
    string FullName,
    SystemRole Role,
    Guid? DepartmentId,
    DepartmentCode? DepartmentCode,
    MemberStatus Status,
    bool MustChangePassword,
    string SecurityStamp);

/// <summary>Outcome of a password sign-in attempt (with lockout state).</summary>
public sealed record PasswordSignInOutcome(bool Succeeded, bool IsLockedOut);

/// <summary>Input for creating a member account.</summary>
public sealed record NewMember(
    string FullName,
    string Email,
    string? PhoneNumber,
    SystemRole Role,
    Guid? DepartmentId,
    DateOnly JoinedOn);

/// <summary>The created member plus the generated temporary password (to email).</summary>
public sealed record CreatedMember(Guid Id, string TemporaryPassword);

/// <summary>Editable member fields (role/department changes rotate the security stamp).</summary>
public sealed record MemberUpdate(
    string FullName,
    string? PhoneNumber,
    SystemRole Role,
    Guid? DepartmentId,
    DateOnly JoinedOn);

/// <summary>
/// Wraps ASP.NET Core Identity (UserManager/SignInManager) so the Application layer can
/// manage members and authenticate without referencing the Infrastructure <c>ApplicationUser</c>.
/// </summary>
public interface IIdentityService
{
    Task<MemberAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<MemberAccount?> FindByIdAsync(Guid memberId, CancellationToken cancellationToken = default);

    /// <summary>Validates the password, incrementing the lockout counter when <paramref name="lockoutOnFailure"/>.</summary>
    Task<PasswordSignInOutcome> CheckPasswordAsync(
        Guid memberId, string password, bool lockoutOnFailure, CancellationToken cancellationToken = default);

    /// <summary>Creates an account with a generated temporary password and <c>MustChangePassword = true</c>.</summary>
    Task<Result<CreatedMember>> CreateMemberAsync(NewMember request, CancellationToken cancellationToken = default);

    /// <summary>Updates editable fields; rotates the security stamp when the role or department changes.</summary>
    Task<Result> UpdateMemberAsync(Guid memberId, MemberUpdate request, CancellationToken cancellationToken = default);

    /// <summary>Updates only the phone number (self-service profile edit).</summary>
    Task<Result> UpdatePhoneAsync(Guid memberId, string? phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>Updates the stored avatar key (self-service profile photo).</summary>
    Task<Result> UpdatePhotoUrlAsync(Guid memberId, string? photoUrl, CancellationToken cancellationToken = default);

    /// <summary>Activates/deactivates a member; deactivation rotates the security stamp.</summary>
    Task<Result> SetStatusAsync(Guid memberId, MemberStatus status, CancellationToken cancellationToken = default);

    /// <summary>Changes the password (validates current + complexity) and clears <c>MustChangePassword</c>.</summary>
    Task<Result> ChangePasswordAsync(
        Guid memberId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>Generates a reset token for an active account; returns <c>null</c> to avoid user enumeration.</summary>
    Task<string?> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Resets the password using a previously issued token and clears <c>MustChangePassword</c>.</summary>
    Task<Result> ResetPasswordAsync(
        string email, string token, string newPassword, CancellationToken cancellationToken = default);
}
