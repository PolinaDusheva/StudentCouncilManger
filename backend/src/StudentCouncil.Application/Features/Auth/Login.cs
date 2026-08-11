using FluentValidation;
using MediatR;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Features.Members;

namespace StudentCouncil.Application.Features.Auth;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthTokensResponse>;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginHandler : IRequestHandler<LoginCommand, AuthTokensResponse>
{
    private readonly IIdentityService _identity;
    private readonly IJwtTokenService _jwt;
    private readonly IRefreshTokenService _refreshTokens;

    public LoginHandler(IIdentityService identity, IJwtTokenService jwt, IRefreshTokenService refreshTokens)
    {
        _identity = identity;
        _jwt = jwt;
        _refreshTokens = refreshTokens;
    }

    public async Task<AuthTokensResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var account = await _identity.FindByEmailAsync(request.Email, cancellationToken);
        if (account is null)
        {
            throw new UnauthorizedException("Invalid email or password.", "invalid_credentials");
        }

        if (account.Status != MemberStatus.Active)
        {
            throw new UnauthorizedException("The account is inactive.", "account_inactive");
        }

        var outcome = await _identity.CheckPasswordAsync(account.Id, request.Password, lockoutOnFailure: true, cancellationToken);
        if (outcome.IsLockedOut)
        {
            throw new LockedException("Too many failed attempts. The account is locked for a few minutes.");
        }

        if (!outcome.Succeeded)
        {
            throw new UnauthorizedException("Invalid email or password.", "invalid_credentials");
        }

        var access = _jwt.CreateAccessToken(account.ToTokenUser());
        var refresh = await _refreshTokens.IssueAsync(account.Id, cancellationToken);

        return new AuthTokensResponse(
            access.Token, refresh.RawToken, access.ExpiresInSeconds, account.MustChangePassword, account.ToSummary());
    }
}
