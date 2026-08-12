using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using Serilog;
using StudentCouncil.Api.Filters;
using StudentCouncil.Api.Health;
using StudentCouncil.Api.Middleware;
using StudentCouncil.Api.Observability;
using StudentCouncil.Application;
using StudentCouncil.Infrastructure;
using StudentCouncil.Infrastructure.Health;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    // Application + Infrastructure layers.
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Behind a reverse proxy / managed ingress (e.g. Azure Container Apps), honour X-Forwarded-* so the
    // app sees the original client IP and HTTPS scheme. Enabled via config in those environments only.
    var behindProxy = builder.Configuration.GetValue("ForwardedHeaders:Enabled", false);
    if (behindProxy)
    {
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            // The managed ingress is the only route to the container (it isn't publicly reachable) and its
            // IP isn't known ahead of time, so trust the headers it sets rather than pinning a proxy address.
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });
    }

    // OpenTelemetry metrics + traces (no-op unless an OTLP endpoint is configured).
    builder.Services.AddObservability(builder.Configuration);

    // MVC controllers (enums serialized as strings to match the JSON API conventions).
    // The password-change gate runs globally as an action filter.
    builder.Services
        .AddControllers(options => options.Filters.Add<RequirePasswordChangeFilter>())
        .AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(ConfigureSwagger);

    // File upload limits (spec 8: 25 MB). A global cap at Kestrel + the multipart body limit;
    // individual upload actions narrow this further with [RequestSizeLimit]. Exceeding the limit
    // surfaces as 413 payload_too_large.
    const long maxUploadBytes = 25L * 1024 * 1024;
    builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = maxUploadBytes);
    builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = maxUploadBytes);

    // Global exception handling -> ProblemDetails.
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // Readiness probes (spec 14, decision #5): DB + Blob + Push, all tagged "ready" so /health filters to
    // them while /health/live stays dependency-free. The connection string is resolved lazily so test/host
    // configuration applied after registration is honoured.
    builder.Services.AddHealthChecks()
        .AddNpgSql(
            connectionStringFactory: sp =>
                sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")
                ?? throw new InvalidOperationException("Connection string 'Default' is not configured."),
            name: "database",
            tags: ["ready", "db"])
        .AddCheck<BlobStorageHealthCheck>("blob", tags: ["ready", "blob"])
        .AddCheck<PushHealthCheck>("push", tags: ["ready", "push"]);

    // Rate limiting: a global fixed window per user (or IP), plus a stricter named
    // policy applied to the auth endpoints (spec 13).
    var globalPerMinute = builder.Configuration.GetValue("RateLimiting:GlobalPerMinute", 100);
    var authPerMinute = builder.Configuration.GetValue("RateLimiting:AuthPerMinute", 10);
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        {
            var partitionKey = httpContext.User.FindFirstValue("sub")
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous";

            return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = globalPerMinute,
                Window = TimeSpan.FromMinutes(1)
            });
        });

        // Auth endpoints are partitioned by IP (callers are typically anonymous).
        options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authPerMinute,
                Window = TimeSpan.FromMinutes(1)
            }));
    });

    // CORS for the browser client (the mobile apps and the Vite dev proxy call the API
    // server-to-server and are unaffected). Origins are configured per environment — an empty
    // list disables cross-origin access entirely rather than falling back to a permissive
    // default. Credentials are not allowed: the SPA authenticates with a bearer token, not
    // cookies, so `Authorization` only needs to be an allowed *request* header.
    var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
    builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
        .WithOrigins(corsOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

    var app = builder.Build();

    // Dev/Staging: apply migrations and seed. Production runs migrations from the pipeline.
    if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
    {
        await app.Services.InitializeDatabaseAsync();
    }
    else
    {
        // Production: the CD pipeline applies the schema (EF migration bundle); still run the idempotent
        // seeder so a fresh deployment has the 4 departments and an initial OrgPresident to sign in with.
        await app.Services.SeedDatabaseAsync();
    }

    // Must run before anything that reads the scheme or client IP (request logging, HSTS, auth).
    if (behindProxy)
    {
        app.UseForwardedHeaders();
    }

    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();

    // Defensive response headers (spec 13); registered early so they cover every downstream response.
    app.UseSecurityHeaders();

    if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // HSTS everywhere except local development (spec 13).
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    // Behind the managed ingress TLS is terminated at the edge (ingress allowInsecure=false), so in-app
    // redirection is redundant and would only bounce internal HTTP health probes; enforce it only when the
    // app is directly exposed.
    if (!behindProxy)
    {
        app.UseHttpsRedirection();
    }

    // Ahead of authentication and the rate limiter so that 401/429 responses still carry the
    // CORS headers — otherwise the browser reports a misleading CORS failure instead of the
    // real status. Preflight requests are answered here and never reach the controllers.
    app.UseCors();

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();

    app.MapControllers();

    // Liveness: no dependencies, just proves the process is up (orchestrator restart signal).
    app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

    // Readiness: DB + Blob + Push, with per-check JSON (orchestrator traffic-gating signal).
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = HealthCheckResponseWriter.WriteAsync
    });

    await app.RunAsync();
}
catch (Exception ex) when (ex.GetType().Name is not ("HostAbortedException" or "StopTheHostException"))
{
    // StopTheHostException/HostAbortedException are thrown by the test host
    // (WebApplicationFactory) to capture the built host; let them propagate.
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    await Log.CloseAndFlushAsync();
}

static void ConfigureSwagger(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options)
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Student Council API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT access token. Example: \"Bearer {token}\"",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", null, null), new List<string>() }
    });
}

/// <summary>Exposed so the integration test project can reference the entry point.</summary>
public partial class Program;
