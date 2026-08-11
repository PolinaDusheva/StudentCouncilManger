using FluentValidation;
using MediatR;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Common.Validation;
using ValidationException = StudentCouncil.Application.Common.Exceptions.ValidationException;

namespace StudentCouncil.Application.Features.Auth;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).Password();
    }
}

public sealed class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityService _identity;
    private readonly IRefreshTokenService _refreshTokens;

    public ChangePasswordHandler(ICurrentUser currentUser, IIdentityService identity, IRefreshTokenService refreshTokens)
    {
        _currentUser = currentUser;
        _identity = identity;
        _refreshTokens = refreshTokens;
    }

    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.Id ?? throw new UnauthorizedException();

        var result = await _identity.ChangePasswordAsync(
            userId, request.CurrentPassword, request.NewPassword, cancellationToken);

        if (!result.Succeeded)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["currentPassword"] = ["The current password is incorrect."]
            });
        }

        // Force a fresh sign-in everywhere; the security stamp was already rotated by the change.
        await _refreshTokens.RevokeAllForMemberAsync(userId, cancellationToken);
    }
}
