using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Application.Common.Files;
using StudentCouncil.Application.Common.Options;
using StudentCouncil.Infrastructure.Audit;
using StudentCouncil.Infrastructure.BackgroundJobs;
using StudentCouncil.Infrastructure.Common;
using StudentCouncil.Infrastructure.Identity;
using StudentCouncil.Infrastructure.Notifications.Email;
using StudentCouncil.Infrastructure.Notifications.Push;
using StudentCouncil.Infrastructure.Options;
using StudentCouncil.Infrastructure.Persistence;
using StudentCouncil.Infrastructure.Persistence.Interceptors;
using StudentCouncil.Infrastructure.Persistence.Seed;
using StudentCouncil.Infrastructure.Storage;

namespace StudentCouncil.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddOptions(services, configuration);
        AddPersistence(services);
        AddIdentityAndAuth(services, configuration);
        AddServices(services);
        AddNotifications(services, configuration);
        AddBackgroundJobs(services, configuration);

        return services;
    }

    private static void AddOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SeedOptions>()
            .Bind(configuration.GetSection(SeedOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<IdentityOptionsConfig>()
            .Bind(configuration.GetSection(IdentityOptionsConfig.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName));

        services.AddOptions<PasswordResetOptions>()
            .Bind(configuration.GetSection(PasswordResetOptions.SectionName));

        // Read lazily (via IOptions in the handlers) so test/host configuration applied after
        // registration wins — the duty norm must be overridable by the integration-test host.
        services.AddOptions<DutyPolicyOptions>()
            .Bind(configuration.GetSection(DutyPolicyOptions.SectionName));

        // Bound lazily (via the options pattern) so configuration applied after registration —
        // e.g. by the integration-test host — is respected.
        services.AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName));
    }

    private static void AddPersistence(IServiceCollection services)
    {
        services.AddScoped<AuditableEntitySaveChangesInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            // Read the connection string lazily so configuration applied after registration
            // (e.g. by the integration-test host) is respected.
            var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")
                ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
            options.AddInterceptors(sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>());
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<DbInitializer>();
    }

    private static void AddIdentityAndAuth(IServiceCollection services, IConfiguration configuration)
    {
        var identityCfg = configuration.GetSection(IdentityOptionsConfig.SectionName).Get<IdentityOptionsConfig>()
            ?? new IdentityOptionsConfig();

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                // Spec 6.4: >= 8 chars, at least one uppercase and one digit.
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;

                options.User.RequireUniqueEmail = true;

                // Spec 6.5: 5 failed attempts -> 15 minute lockout.
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = identityCfg.MaxFailedAttempts;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(identityCfg.LockoutMinutes);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        // Configure the bearer options from JwtOptions lazily (via the options pattern) so
        // configuration applied after registration — e.g. by the integration-test host — wins.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                // Keep claim names exactly as emitted (short form, no inbound remapping).
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    RoleClaimType = "role",
                    NameClaimType = "name",
                    ClockSkew = TimeSpan.FromSeconds(30)
                };

                // Re-check the `stamp` claim against the stored SecurityStamp on every request so
                // deactivation / role change / password change invalidate already-issued access tokens.
                bearer.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ValidateSecurityStampAsync
                };
            });

        AddAuthorizationPolicies(services);
    }

    private static void AddAuthorizationPolicies(IServiceCollection services)
    {
        var orgLeadership = new[] { nameof(SystemRole.OrgPresident), nameof(SystemRole.OrgVicePresident) };
        var deptTaskCreators = new[]
        {
            nameof(SystemRole.DeptPresident),
            nameof(SystemRole.DeptVicePresident),
            nameof(SystemRole.OrgPresident),
            nameof(SystemRole.OrgVicePresident)
        };

        // Every role except a plain Member may create events (functional 8.2); the fine-grained
        // edit/delete rules live in EventAccess (decision #2).
        var eventManagers = new[]
        {
            nameof(SystemRole.DeptSecretary),
            nameof(SystemRole.DeptVicePresident),
            nameof(SystemRole.DeptPresident),
            nameof(SystemRole.OrgSecretary),
            nameof(SystemRole.OrgVicePresident),
            nameof(SystemRole.OrgPresident)
        };

        services.AddAuthorizationBuilder()
            .AddPolicy("OrgLeadership", p => p.RequireRole(orgLeadership))
            .AddPolicy("OrgPresidentOnly", p => p.RequireRole(nameof(SystemRole.OrgPresident)))
            .AddPolicy("CanManageMembers", p => p.RequireRole(orgLeadership))
            .AddPolicy("CanManageDuties", p => p.RequireRole(orgLeadership))
            .AddPolicy("CanManageBudget", p => p.RequireRole(orgLeadership))
            .AddPolicy("CanCreateOrgTask", p => p.RequireRole(orgLeadership))
            .AddPolicy("CanCreateDeptTask", p => p.RequireRole(deptTaskCreators))
            .AddPolicy("CanManageEvents", p => p.RequireRole(eventManagers));
    }

    private static async Task ValidateSecurityStampAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;
        var stamp = principal?.FindFirstValue("stamp");
        var sub = principal?.FindFirstValue("sub");

        if (string.IsNullOrEmpty(stamp) || !Guid.TryParse(sub, out var userId))
        {
            context.Fail("The access token is missing required claims.");
            return;
        }

        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null
            || user.Status != MemberStatus.Active
            || !string.Equals(user.SecurityStamp, stamp, StringComparison.Ordinal))
        {
            context.Fail("The access token is no longer valid.");
        }
    }

    private static void AddServices(IServiceCollection services)
    {
        services.AddSingleton<IDateTime, SystemDateTime>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IMemberDirectory, MemberDirectory>();

        // Audit trail seam (spec 14): stages AuditLog rows in the current unit of work, committed with
        // the handler's own SaveChangesAsync so the audit entry and the action are atomic (decision #1).
        services.AddScoped<IAuditRecorder, AuditRecorder>();

        // File storage provider switch (spec 8): Local for dev/test, Azure Blob for Staging/Production.
        // The provider is read lazily so host/test configuration applied after registration wins; the
        // Azure client (which validates its connection string) is only constructed when actually selected.
        services.AddScoped<LocalFileStorage>();
        services.AddScoped<AzureBlobFileStorage>();
        services.AddScoped<IFileStorage>(sp =>
        {
            var provider = sp.GetRequiredService<IOptions<StorageOptions>>().Value.Provider;
            return string.Equals(provider, "AzureBlob", StringComparison.OrdinalIgnoreCase)
                ? sp.GetRequiredService<AzureBlobFileStorage>()
                : sp.GetRequiredService<LocalFileStorage>();
        });

        // Surface the configured upload limits to the Application layer (resolved lazily from options).
        services.AddScoped(sp =>
        {
            var storage = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
            return new StorageLimits(storage.MaxFileSizeBytes, storage.MaxAvatarSizeBytes);
        });

        // Development/Staging log emails (so temp passwords / reset links are inspectable);
        // only Production sends real SMTP.
        services.AddScoped<IEmailSender>(sp =>
        {
            var environment = sp.GetRequiredService<IHostEnvironment>();
            return environment.IsProduction()
                ? new SmtpEmailSender(
                    sp.GetRequiredService<IOptions<EmailOptions>>(),
                    sp.GetRequiredService<ILogger<SmtpEmailSender>>())
                : new LoggingEmailSender(sp.GetRequiredService<ILogger<LoggingEmailSender>>());
        });
    }

    private static void AddNotifications(IServiceCollection services, IConfiguration configuration)
    {
        // Bound lazily (options pattern) so host/test configuration applied after registration wins.
        services.AddOptions<PushOptions>().Bind(configuration.GetSection(PushOptions.SectionName));

        // The single handler/job-facing seam: persists in-app rows + delegates to push (decision #1).
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

        // Per-platform providers (only exercised by the Production push service; harmless to register always).
        services.AddScoped<IPushProvider, FcmPushProvider>();
        services.AddScoped<IPushProvider, ApnsPushProvider>();

        // Dev/Staging/test log instead of sending; only Production routes to the real FCM/APNs providers
        // (decision #3, mirroring the IEmailSender environment-switch).
        services.AddScoped<IPushNotificationService>(sp =>
        {
            var environment = sp.GetRequiredService<IHostEnvironment>();
            return environment.IsProduction()
                ? new PushNotificationService(
                    sp.GetRequiredService<IAppDbContext>(),
                    sp.GetServices<IPushProvider>(),
                    sp.GetRequiredService<ILogger<PushNotificationService>>())
                : new LoggingPushNotificationService(sp.GetRequiredService<ILogger<LoggingPushNotificationService>>());
        });
    }

    private static void AddBackgroundJobs(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BackgroundJobOptions>().Bind(configuration.GetSection(BackgroundJobOptions.SectionName));

        // Always register the concrete jobs (so tests can resolve and drive a single ExecuteTickAsync),
        // but only host the timers when enabled (decision #4/#6 — keeps the integration test host quiet).
        services.AddSingleton<TaskDueSoonJob>();
        services.AddSingleton<TaskOverdueJob>();
        services.AddSingleton<EventReminderJob>();
        services.AddSingleton<RefreshTokenCleanupJob>();
        services.AddSingleton<DutyMonthlyArchiveJob>();
        services.AddSingleton<OrphanFileCleanupJob>();

        var enabled = configuration.GetValue($"{BackgroundJobOptions.SectionName}:Enabled", true);
        if (!enabled)
        {
            return;
        }

        services.AddHostedService(sp => sp.GetRequiredService<TaskDueSoonJob>());
        services.AddHostedService(sp => sp.GetRequiredService<TaskOverdueJob>());
        services.AddHostedService(sp => sp.GetRequiredService<EventReminderJob>());
        services.AddHostedService(sp => sp.GetRequiredService<RefreshTokenCleanupJob>());
        services.AddHostedService(sp => sp.GetRequiredService<DutyMonthlyArchiveJob>());
        services.AddHostedService(sp => sp.GetRequiredService<OrphanFileCleanupJob>());
    }
}
