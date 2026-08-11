using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StudentCouncil.Infrastructure.Notifications.Push;

namespace StudentCouncil.Infrastructure.Health;

/// <summary>
/// Readiness check for push (spec 9/14, decision #6). It never sends a real notification: outside Production
/// push runs in logging mode and is always healthy; in Production it verifies that at least one provider
/// credential (FCM or APNs) is configured — i.e. the real provider could initialise — without doing any I/O.
/// </summary>
public sealed class PushHealthCheck : IHealthCheck
{
    private readonly IHostEnvironment _environment;
    private readonly PushOptions _options;

    public PushHealthCheck(IHostEnvironment environment, IOptions<PushOptions> options)
    {
        _environment = environment;
        _options = options.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!_environment.IsProduction())
        {
            return Task.FromResult(HealthCheckResult.Healthy("Push runs in logging mode outside Production."));
        }

        var fcmConfigured = !string.IsNullOrWhiteSpace(_options.Fcm.ServiceAccountJson);
        var apnsConfigured =
            !string.IsNullOrWhiteSpace(_options.Apns.P8Key) &&
            !string.IsNullOrWhiteSpace(_options.Apns.KeyId) &&
            !string.IsNullOrWhiteSpace(_options.Apns.TeamId) &&
            !string.IsNullOrWhiteSpace(_options.Apns.BundleId);

        // Push delivery is an optional, deferred capability (the FCM/APNs SDKs aren't wired yet). Missing
        // credentials must not fail readiness/take the app out of rotation — report Degraded (HTTP 200), so
        // /health still surfaces the gap without reporting the whole service as down.
        return Task.FromResult(fcmConfigured || apnsConfigured
            ? HealthCheckResult.Healthy("Push provider credentials are configured.")
            : HealthCheckResult.Degraded("Push delivery is deferred — no FCM/APNs credentials configured."));
    }
}
