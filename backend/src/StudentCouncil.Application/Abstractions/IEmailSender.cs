namespace StudentCouncil.Application.Abstractions;

/// <summary>Outbound email. Implementation arrives in Phase 2 (temp password, reset link).</summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
