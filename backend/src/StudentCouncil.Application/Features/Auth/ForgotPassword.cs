using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Email;
using StudentCouncil.Application.Common.Options;

namespace StudentCouncil.Application.Features.Auth;

public sealed record ForgotPasswordCommand(string Email) : IRequest;

public sealed class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordValidator() => RuleFor(x => x.Email).NotEmpty().EmailAddress();
}

public sealed class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IIdentityService _identity;
    private readonly IEmailSender _email;
    private readonly PasswordResetOptions _options;

    public ForgotPasswordHandler(IIdentityService identity, IEmailSender email, IOptions<PasswordResetOptions> options)
    {
        _identity = identity;
        _email = email;
        _options = options.Value;
    }

    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        // Returns null for unknown/inactive accounts — we always respond 200 (no enumeration).
        var token = await _identity.GeneratePasswordResetTokenAsync(request.Email, cancellationToken);
        if (token is null)
        {
            return;
        }

        var link = $"{_options.ResetUrlBase}" +
                   $"?email={Uri.EscapeDataString(request.Email)}" +
                   $"&token={Uri.EscapeDataString(token)}";

        var message = EmailTemplates.PasswordReset(request.Email, link, _options.LinkValidMinutes);
        await _email.SendAsync(request.Email, message.Subject, message.HtmlBody, cancellationToken);
    }
}
