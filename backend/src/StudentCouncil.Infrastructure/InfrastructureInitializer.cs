using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentCouncil.Infrastructure.Persistence;
using StudentCouncil.Infrastructure.Persistence.Seed;

namespace StudentCouncil.Infrastructure;

public static class InfrastructureInitializer
{
    /// <summary>
    /// Applies pending migrations and runs the idempotent seeder. Intended for
    /// Development/Staging startup; in production migrations run from the pipeline.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(cancellationToken);

        var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
        await initializer.SeedAsync(cancellationToken);
    }

    /// <summary>
    /// Runs the idempotent seeder only (departments + initial OrgPresident). Used on Production
    /// startup, where the schema is applied separately by the CD migration bundle.
    /// </summary>
    public static async Task SeedDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
        await initializer.SeedAsync(cancellationToken);
    }
}
