using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace StudentCouncil.Api.Health;

/// <summary>Writes the readiness report as JSON with per-check results (spec 14, plan §6). The HTTP status
/// code is set by the health-check middleware (200 Healthy/Degraded, 503 Unhealthy); this only renders the body.</summary>
public static class HealthCheckResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            results = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description
                })
        };

        return context.Response.WriteAsJsonAsync(payload);
    }
}
