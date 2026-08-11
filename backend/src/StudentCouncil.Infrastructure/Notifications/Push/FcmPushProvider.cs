using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace StudentCouncil.Infrastructure.Notifications.Push;

/// <summary>
/// Android delivery via FCM. The real Firebase Admin SDK call is wired once credentials exist
/// (decision #3 / risk note): until then this honest placeholder logs rather than claiming delivery, and
/// reports no invalid tokens. Only the body of <see cref="SendAsync"/> changes when the SDK lands.
/// </summary>
public sealed class FcmPushProvider : IPushProvider
{
    private readonly PushOptions _options;
    private readonly ILogger<FcmPushProvider> _logger;

    public FcmPushProvider(IOptions<PushOptions> options, ILogger<FcmPushProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public DevicePlatform Platform => DevicePlatform.Android;

    public Task<IReadOnlyCollection<string>> SendAsync(
        IReadOnlyCollection<string> tokens, PushMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Fcm.ServiceAccountJson))
        {
            _logger.LogWarning(
                "FCM is not configured; skipping {Count} Android token(s) for {Type}.", tokens.Count, message.Type);
        }
        else
        {
            _logger.LogWarning(
                "FCM delivery is not yet wired to the Firebase Admin SDK; {Count} Android token(s) for {Type} were not sent.",
                tokens.Count, message.Type);
        }

        return Task.FromResult<IReadOnlyCollection<string>>([]);
    }
}
