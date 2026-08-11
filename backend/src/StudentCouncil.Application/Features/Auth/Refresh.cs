using FluentValidation;
using MediatR;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Features.Members;

namespace StudentCouncil.Application.Features.Auth;

public sealed record RefreshCommand(string RefreshToken) : IRequest<AuthTokensResponse>;

public sealed class RefreshValidator : AbstractValidator<RefreshCommand>
{
    public RefreshValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}

public sealed class RefreshHandler : IRequestHandler<RefreshCommand, AuthTokensResponse>
{
    private readonly IIdentityService _identity;
    private readonly IJwtTokenService _jwt;
    private readonly IRefreshTokenService _refreshTokens;

    public RefreshHandler(IIdentityService identity, IJwtTokenService jwt, IRefreshTokenService refreshTokens)
    {
        _identity = identity;
        _jwt = jwt;
        _refreshTokens = refreshTokens;
    }

    public async Task<AuthTokensResponse> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        // Rotates (and validates expiry/reuse); throws UnauthorizedException on an invalid token.
        var rotated = await _refreshTokens.RotateAsync(request.RefreshToken, cancellationToken);

        var account = await _identity.FindByIdAsync(rotated.MemberId, cancellationToken);
        if (account is null || account.Status != MemberStatus.Active)
        {
            // A deactivated member must not be able to keep refreshing.
            await _refreshTokens.RevokeAllForMemberAsync(rotated.MemberId, cancellationToken);
            throw new UnauthorizedException("The account is inactive.", "account_inactive");
        }

        var access = _jwt.CreateAccessToken(account.ToTokenUser());

        return new AuthTokensResponse(
            access.Token, rotated.RawToken, access.ExpiresInSeconds, account.MustChangePassword, account.ToSummary());
    }
}
