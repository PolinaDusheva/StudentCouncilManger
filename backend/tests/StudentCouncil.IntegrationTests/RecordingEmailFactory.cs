using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using StudentCouncil.Application.Abstractions;

namespace StudentCouncil.IntegrationTests;

/// <summary>Captures outbound emails so tests can read temporary passwords / reset links.</summary>
public sealed class RecordingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailRecord> _messages = new();

    public IReadOnlyCollection<EmailRecord> Messages => _messages;

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        _messages.Enqueue(new EmailRecord(to, subject, htmlBody));
        return Task.CompletedTask;
    }
}

public sealed record EmailRecord(string To, string Subject, string Body);

/// <summary>Factory variant that swaps the email sender for an in-memory recorder.</summary>
public sealed class RecordingEmailFactory : SmokeTestFactory
{
    public RecordingEmailSender Email { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        // Registered last, so it wins over the environment-selected sender.
        builder.ConfigureTestServices(services => services.AddSingleton<IEmailSender>(Email));
    }
}
