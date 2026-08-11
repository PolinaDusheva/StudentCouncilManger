using FluentValidation;
using MediatR;
using StudentCouncil.Application.Abstractions;

namespace StudentCouncil.Application.Features.Auth;

public sealed record LogoutCommand(string RefreshToken) : IRequest;

public sealed class LogoutValidator : AbstractValidator<LogoutCommand>
{
    public LogoutValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}

public sealed class LogoutHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenService _refreshTokens;

    public LogoutHandler(IRefreshTokenService refreshTokens) => _refreshTokens = refreshTokens;

    public Task Handle(LogoutCommand request, CancellationToken cancellationToken) =>
        _refreshTokens.RevokeAsync(request.RefreshToken, cancellationToken);
}
