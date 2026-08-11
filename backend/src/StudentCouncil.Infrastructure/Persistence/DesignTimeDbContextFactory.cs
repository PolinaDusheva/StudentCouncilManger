using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StudentCouncil.Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef</c> build the context without starting the Api. Connection string
/// comes from <c>ConnectionStrings__Default</c> (env) or a local dev default.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string LocalDefault =
        "Host=localhost;Port=5432;Database=studentcouncil;Username=postgres;Password=postgres";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Default") ?? LocalDefault;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options);
    }
}
