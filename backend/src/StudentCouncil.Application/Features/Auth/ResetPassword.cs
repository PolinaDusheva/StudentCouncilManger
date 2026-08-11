using FluentValidation;
using MediatR;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Common.Validation;

namespace StudentCouncil.Application.Features.Auth;

public sealed record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest;

public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).Password();
    }
}

public sealed class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IIdentityService _identity;
    private readonly IRefreshTokenService _refreshTokens;

    public ResetPasswordHandler(IIdentityService identity, IRefreshTokenService refreshTokens)
    {
        _identity = identity;
        _refreshTokens = refreshTokens;
    }

    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var result = await _identity.ResetPasswordAsync(
            request.Email, request.Token, request.NewPassword, cancellationToken);

        if (!result.Succeeded)
        {
            throw new BadRequestException("The reset token is invalid or has expired.", "invalid_reset_token");
        }

        // Stamp was rotated by the reset; also drop any refresh tokens so old sessions end.
        var account = await _identity.FindByEmailAsync(request.Email, cancellationToken);
        if (account is not null)
        {
            await _refreshTokens.RevokeAllForMemberAsync(account.Id, cancellationToken);
        }
    }
}
