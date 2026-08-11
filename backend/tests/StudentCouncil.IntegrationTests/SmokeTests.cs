using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentCouncil.Domain.Enums;
using StudentCouncil.Infrastructure.Identity;
using StudentCouncil.Infrastructure.Persistence;

namespace StudentCouncil.IntegrationTests;

public class SmokeTests : IClassFixture<SmokeTestFactory>
{
    private readonly SmokeTestFactory _factory;

    public SmokeTests(SmokeTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_endpoint_returns_healthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Readiness now returns per-check JSON; the overall status is still "Healthy".
        (await response.Content.ReadAsStringAsync()).Should().Contain("Healthy");
    }

    [Fact]
    public async Task Startup_applies_migration_and_seeds_departments_and_admin()
    {
        // Forces host startup, which applies the migration and runs the seeder.
        _ = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Departments.CountAsync()).Should().Be(4);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByEmailAsync(SmokeTestFactory.AdminEmail);

        admin.Should().NotBeNull();
        admin!.Role.Should().Be(SystemRole.OrgPresident);
        admin.MustChangePassword.Should().BeTrue();
    }
}
