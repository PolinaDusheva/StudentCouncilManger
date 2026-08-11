using Microsoft.Extensions.Logging;
using StudentCouncil.Application.Abstractions;

namespace StudentCouncil.Infrastructure.Notifications.Email;

/// <summary>
/// Development/Staging email sender: logs the recipient and body instead of sending.
/// Lets temporary passwords and reset links be inspected without a real SMTP server.
/// </summary>
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger) => _logger = logger;

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[DEV EMAIL] To: {To} | Subject: {Subject}\n{Body}", to, subject, htmlBody);
        return Task.CompletedTask;
    }
}
