using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Models;
using StudentCouncil.Infrastructure.Persistence;

namespace StudentCouncil.Infrastructure.Identity;

/// <summary>
/// ASP.NET Core Identity adapter for the Application layer. Returns Identity-free
/// records/results so handlers never see <see cref="ApplicationUser"/>.
/// </summary>
public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AppDbContext _db;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        AppDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
    }

    public async Task<MemberAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user is null ? null : await ToAccountAsync(user, cancellationToken);
    }

    public async Task<MemberAccount?> FindByIdAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(memberId.ToString());
        return user is null ? null : await ToAccountAsync(user, cancellationToken);
    }

    public async Task<PasswordSignInOutcome> CheckPasswordAsync(
        Guid memberId, string password, bool lockoutOnFailure, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(memberId.ToString());
        if (user is null)
        {
            return new PasswordSignInOutcome(false, false);
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure);
        return new PasswordSignInOutcome(result.Succeeded, result.IsLockedOut);
    }

    public async Task<Result<CreatedMember>> CreateMemberAsync(NewMember request, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            Role = request.Role,
            DepartmentId = request.DepartmentId,
            Status = MemberStatus.Active,
            JoinedOn = request.JoinedOn,
            MustChangePassword = true
        };

        var temporaryPassword = TemporaryPassword.Generate();
        var result = await _userManager.CreateAsync(user, temporaryPassword);

        return result.Succeeded
            ? Result<CreatedMember>.Success(new CreatedMember(user.Id, temporaryPassword))
            : Result<CreatedMember>.Failure(Describe(result), CodeOf(result));
    }

    public async Task<Result> UpdateMemberAsync(Guid memberId, MemberUpdate request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(memberId.ToString());
        if (user is null)
        {
            return Result.Failure("Member not found.", "not_found");
        }

        var roleOrDepartmentChanged = user.Role != request.Role || user.DepartmentId != request.DepartmentId;

        user.FullName = request.FullName;
        user.PhoneNumber = request.PhoneNumber;
        user.Role = request.Role;
        user.DepartmentId = request.DepartmentId;
        user.JoinedOn = request.JoinedOn;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return Result.Failure(Describe(result), CodeOf(result));
        }

        // A new role/department must take effect on the next token refresh.
        if (roleOrDepartmentChanged)
        {
            await _userManager.UpdateSecurityStampAsync(user);
        }

        return Result.Success();
    }

    public async Task<Result> UpdatePhoneAsync(Guid memberId, string? phoneNumber, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(memberId.ToString());
        if (user is null)
        {
            return Result.Failure("Member not found.", "not_found");
        }

        user.PhoneNumber = phoneNumber;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded ? Result.Success() : Result.Failure(Describe(result), CodeOf(result));
    }

    public async Task<Result> UpdatePhotoUrlAsync(Guid memberId, string? photoUrl, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(memberId.ToString());
        if (user is null)
        {
            return Result.Failure("Member not found.", "not_found");
        }

        user.PhotoUrl = photoUrl;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded ? Result.Success() : Result.Failure(Describe(result), CodeOf(result));
    }

    public async Task<Result> SetStatusAsync(Guid memberId, MemberStatus status, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(memberId.ToString());
        if (user is null)
        {
            return Result.Failure("Member not found.", "not_found");
        }

        user.Status = status;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return Result.Failure(Describe(result), CodeOf(result));
        }

        // Deactivation must invalidate any access token already issued to this member.
        if (status == MemberStatus.Inactive)
        {
            await _userManager.UpdateSecurityStampAsync(user);
        }

        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(
        Guid memberId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(memberId.ToString());
        if (user is null)
        {
            return Result.Failure("Member not found.", "not_found");
        }

        // ChangePasswordAsync validates the current password and rotates the security stamp.
        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            return Result.Failure(Describe(result), CodeOf(result));
        }

        if (user.MustChangePassword)
        {
            user.MustChangePassword = false;
            await _userManager.UpdateAsync(user);
        }

        return Result.Success();
    }

    public async Task<string?> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || user.Status != MemberStatus.Active)
        {
            return null;
        }

        return await _userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<Result> ResetPasswordAsync(
        string email, string token, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            // Same failure for unknown email and bad token (no enumeration via reset).
            return Result.Failure("The reset token is invalid or has expired.", "invalid_reset_token");
        }

        // ResetPasswordAsync rotates the security stamp on success.
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            return Result.Failure("The reset token is invalid or has expired.", "invalid_reset_token");
        }

        if (user.MustChangePassword)
        {
            user.MustChangePassword = false;
            await _userManager.UpdateAsync(user);
        }

        return Result.Success();
    }

    private async Task<MemberAccount> ToAccountAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        DepartmentCode? departmentCode = null;
        if (user.DepartmentId is { } departmentId)
        {
            departmentCode = await _db.Departments
                .Where(d => d.Id == departmentId)
                .Select(d => (DepartmentCode?)d.Code)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new MemberAccount(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            user.Role,
            user.DepartmentId,
            departmentCode,
            user.Status,
            user.MustChangePassword,
            user.SecurityStamp ?? string.Empty);
    }

    private static string Describe(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(e => e.Description));

    private static string? CodeOf(IdentityResult result) =>
        result.Errors.FirstOrDefault()?.Code;
}
