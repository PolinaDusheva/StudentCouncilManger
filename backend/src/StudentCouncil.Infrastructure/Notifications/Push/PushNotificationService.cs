using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Notifications;

namespace StudentCouncil.Infrastructure.Notifications.Push;

/// <summary>
/// Production push delivery (spec 9, decision #2): resolves the recipients' device tokens, groups them by
/// platform, hands each group to the matching <see cref="IPushProvider"/>, and prunes any tokens the
/// gateway reported as invalid/expired.
/// </summary>
public sealed class PushNotificationService : IPushNotificationService
{
    private readonly IAppDbContext _db;
    private readonly IReadOnlyDictionary<DevicePlatform, IPushProvider> _providers;
    private readonly ILogger<PushNotificationService> _logger;

    public PushNotificationService(
        IAppDbContext db, IEnumerable<IPushProvider> providers, ILogger<PushNotificationService> logger)
    {
        _db = db;
        _providers = providers.ToDictionary(p => p.Platform);
        _logger = logger;
    }

    public async Task SendAsync(
        IReadOnlyCollection<Guid> memberIds,
        NotificationType type,
        string title,
        string body,
        NotificationPayload payload,
        CancellationToken cancellationToken = default)
    {
        if (memberIds.Count == 0)
        {
            return;
        }

        var tokens = await _db.DeviceTokens
            .Where(d => memberIds.Contains(d.MemberId))
            .ToListAsync(cancellationToken);
        if (tokens.Count == 0)
        {
            return;
        }

        var message = new PushMessage(type, title, body, payload);
        var invalidTokens = new HashSet<string>();

        foreach (var group in tokens.GroupBy(t => t.Platform))
        {
            if (!_providers.TryGetValue(group.Key, out var provider))
            {
                _logger.LogWarning("No push provider registered for platform {Platform}.", group.Key);
                continue;
            }

            var groupTokens = group.Select(t => t.Token).ToList();
            var invalid = await provider.SendAsync(groupTokens, message, cancellationToken);
            foreach (var token in invalid)
            {
                invalidTokens.Add(token);
            }
        }

        if (invalidTokens.Count == 0)
        {
            return;
        }

        var stale = tokens.Where(t => invalidTokens.Contains(t.Token)).ToList();
        _db.DeviceTokens.RemoveRange(stale);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
