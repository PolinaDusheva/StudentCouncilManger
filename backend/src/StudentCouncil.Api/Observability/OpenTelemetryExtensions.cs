using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace StudentCouncil.Api.Observability;

/// <summary>
/// Wires OpenTelemetry metrics + traces (spec 14, decision #7). The OTLP exporter is attached only when
/// <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is configured; with no endpoint there is no reader/exporter, so the
/// pipeline is effectively a no-op and dev/test/CI carry zero external dependency. Serilog still owns logs.
/// </summary>
public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var exportEnabled = !string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("StudentCouncil.Api"))
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (exportEnabled)
                {
                    metrics.AddOtlpExporter();
                }
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation();

                if (exportEnabled)
                {
                    tracing.AddOtlpExporter();
                }
            });

        return services;
    }
}
