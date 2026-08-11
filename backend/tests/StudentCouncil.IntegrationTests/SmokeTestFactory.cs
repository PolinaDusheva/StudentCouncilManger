using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace StudentCouncil.IntegrationTests;

/// <summary>
/// Boots the real app against a throwaway local Postgres database. Uses the
/// Staging environment so the app still migrates + seeds on startup but does
/// NOT load the developer's user secrets (which point at the dev database).
/// In CI this can be swapped for a Testcontainers-backed connection string.
/// </summary>
public class SmokeTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string MaintenanceConnectionString =
        "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";

    // Unique per factory instance so independent test classes get isolated, parallel-safe databases.
    private readonly string _testDatabaseName = $"studentcouncil_inttest_{Guid.NewGuid():N}";

    // Per-factory file storage root so uploaded documents/avatars stay isolated and disposable.
    private readonly string _storageRoot = Path.Combine(Path.GetTempPath(), $"sc-inttest-{Guid.NewGuid():N}");

    public const string AdminEmail = "int-admin@ue-varna.bg";
    public const string AdminInitialPassword = "IntegrationPass1";

    private string TestConnectionString =>
        $"Host=localhost;Port=5432;Database={_testDatabaseName};Username=postgres;Password=postgres";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Staging");

        // Added after the default sources, so these win over appsettings.json.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = TestConnectionString,
                ["Jwt:SigningKey"] = "integration-tests-signing-key-32-bytes-min!!",
                ["Seed:AdminEmail"] = AdminEmail,
                ["Seed:AdminPassword"] = AdminInitialPassword,
                ["Storage:LocalRootPath"] = _storageRoot,
                // Keep the background-job timers off in tests; ticks are driven explicitly.
                ["BackgroundJobs:Enabled"] = "false"
            });
        });
    }

    public async Task InitializeAsync() => await RecreateDatabaseAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DropDatabaseAsync();
        if (Directory.Exists(_storageRoot))
        {
            Directory.Delete(_storageRoot, recursive: true);
        }

        await base.DisposeAsync();
    }

    private async Task RecreateDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(MaintenanceConnectionString);
        await connection.OpenAsync();
        await ExecuteAsync(connection, $"DROP DATABASE IF EXISTS \"{_testDatabaseName}\" WITH (FORCE);");
        await ExecuteAsync(connection, $"CREATE DATABASE \"{_testDatabaseName}\";");
    }

    private async Task DropDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(MaintenanceConnectionString);
        await connection.OpenAsync();
        await ExecuteAsync(connection, $"DROP DATABASE IF EXISTS \"{_testDatabaseName}\" WITH (FORCE);");
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
