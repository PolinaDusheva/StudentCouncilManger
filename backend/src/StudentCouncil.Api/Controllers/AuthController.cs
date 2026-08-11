using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudentCouncil.Api.Filters;
using StudentCouncil.Application.Features.Auth;

namespace StudentCouncil.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[Authorize]
[EnableRateLimiting("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender) => _sender = sender;

    /// <summary>Email + password → access &amp; refresh tokens.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthTokensResponse>> Login(LoginCommand command, CancellationToken cancellationToken)
        => Ok(await _sender.Send(command, cancellationToken));

    /// <summary>Refresh token → a new rotated token pair.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthTokensResponse>> Refresh(RefreshCommand command, CancellationToken cancellationToken)
        => Ok(await _sender.Send(command, cancellationToken));

    /// <summary>Revokes the supplied refresh token (idempotent).</summary>
    [HttpPost("logout")]
    [BypassPasswordChangeGate]
    public async Task<IActionResult> Logout(LogoutCommand command, CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Changes the current user's password and clears the must-change flag.</summary>
    [HttpPost("change-password")]
    [BypassPasswordChangeGate]
    public async Task<IActionResult> ChangePassword(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Sends a reset link if the account exists (always 200, no enumeration).</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
        return Ok();
    }

    /// <summary>Resets the password using an emailed token.</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Current user profile, role, department and permission flags.</summary>
    [HttpGet("me")]
    [BypassPasswordChangeGate]
    public async Task<ActionResult<MeResponse>> Me(CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetMeQuery(), cancellationToken));
}
