using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudentCouncil.Application.Abstractions;

namespace StudentCouncil.Infrastructure.Notifications.Email;

/// <summary>Production email sender over SMTP (HTML body).</summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        using var message = new MailMessage(_options.From, to, subject, htmlBody) { IsBodyHtml = true };
        using var client = new SmtpClient(_options.Smtp.Host, _options.Smtp.Port)
        {
            EnableSsl = _options.Smtp.UseStartTls,
            Credentials = string.IsNullOrEmpty(_options.Smtp.Username)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_options.Smtp.Username, _options.Smtp.Password)
        };

        await client.SendMailAsync(message, cancellationToken);
        _logger.LogInformation("Sent email to {To} with subject {Subject}", to, subject);
    }
}
