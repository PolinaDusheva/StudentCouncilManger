using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Notifications;
using StudentCouncil.Domain.Enums;

namespace StudentCouncil.IntegrationTests;

/// <summary>Captures dispatched push calls so tests can assert recipients/type without a real gateway.</summary>
public sealed class RecordingPushNotificationService : IPushNotificationService
{
    private readonly ConcurrentQueue<PushRecord> _sent = new();

    public IReadOnlyCollection<PushRecord> Sent => _sent;

    public Task SendAsync(
        IReadOnlyCollection<Guid> memberIds,
        NotificationType type,
        string title,
        string body,
        NotificationPayload payload,
        CancellationToken cancellationToken = default)
    {
        _sent.Enqueue(new PushRecord(memberIds.ToList(), type, title, body, payload));
        return Task.CompletedTask;
    }
}

public sealed record PushRecord(
    IReadOnlyList<Guid> MemberIds, NotificationType Type, string Title, string Body, NotificationPayload Payload);

/// <summary>
/// Factory variant that records both outbound emails and push dispatches. Background-job timers are
/// already disabled by <see cref="SmokeTestFactory"/>, so tests drive job ticks deterministically.
/// </summary>
public sealed class RecordingNotificationsFactory : SmokeTestFactory
{
    public RecordingEmailSender Email { get; } = new();
    public RecordingPushNotificationService Push { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        // Registered last so these win over the environment-selected services.
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IEmailSender>(Email);
            services.AddScoped<IPushNotificationService>(_ => Push);
        });
    }
}
