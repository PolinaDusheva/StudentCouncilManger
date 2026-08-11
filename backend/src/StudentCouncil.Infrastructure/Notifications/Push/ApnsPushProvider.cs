using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace StudentCouncil.Infrastructure.Notifications.Push;

/// <summary>
/// iOS delivery via APNs. Like <see cref="FcmPushProvider"/>, this is an honest placeholder until the
/// real APNs (.p8 token) integration is wired with credentials (decision #3 / risk note).
/// </summary>
public sealed class ApnsPushProvider : IPushProvider
{
    private readonly PushOptions _options;
    private readonly ILogger<ApnsPushProvider> _logger;

    public ApnsPushProvider(IOptions<PushOptions> options, ILogger<ApnsPushProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public DevicePlatform Platform => DevicePlatform.iOS;

    public Task<IReadOnlyCollection<string>> SendAsync(
        IReadOnlyCollection<string> tokens, PushMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Apns.P8Key))
        {
            _logger.LogWarning(
                "APNs is not configured; skipping {Count} iOS token(s) for {Type}.", tokens.Count, message.Type);
        }
        else
        {
            _logger.LogWarning(
                "APNs delivery is not yet wired; {Count} iOS token(s) for {Type} were not sent.",
                tokens.Count, message.Type);
        }

        return Task.FromResult<IReadOnlyCollection<string>>([]);
    }
}
