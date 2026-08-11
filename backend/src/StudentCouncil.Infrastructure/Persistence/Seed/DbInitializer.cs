using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Domain.Entities;
using StudentCouncil.Infrastructure.Identity;
using StudentCouncil.Infrastructure.Options;

namespace StudentCouncil.Infrastructure.Persistence.Seed;

/// <summary>Idempotent seeder: ensures the 4 departments and an initial OrgPresident exist.</summary>
public sealed class DbInitializer
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SeedOptions _seed;
    private readonly IDateTime _clock;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DbInitializer> _logger;

    public DbInitializer(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        IOptions<SeedOptions> seed,
        IDateTime clock,
        IHostEnvironment environment,
        ILogger<DbInitializer> logger)
    {
        _db = db;
        _userManager = userManager;
        _seed = seed.Value;
        _clock = clock;
        _environment = environment;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedDepartmentsAsync(cancellationToken);
        await SeedAdminAsync(cancellationToken);
    }

    private async Task SeedDepartmentsAsync(CancellationToken cancellationToken)
    {
        var added = false;
        foreach (var code in Enum.GetValues<DepartmentCode>())
        {
            if (!await _db.Departments.AnyAsync(d => d.Code == code, cancellationToken))
            {
                _db.Departments.Add(new Department
                {
                    Id = Guid.NewGuid(),
                    Code = code,
                    Name = DefaultName(code)
                });
                added = true;
            }
        }

        if (added)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded missing departments.");
        }
    }

    private async Task SeedAdminAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_seed.AdminEmail))
        {
            _logger.LogWarning("Seed:AdminEmail is not configured; skipping initial OrgPresident seeding.");
            return;
        }

        var existing = await _userManager.FindByEmailAsync(_seed.AdminEmail);
        if (existing is not null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = _seed.AdminEmail,
            Email = _seed.AdminEmail,
            EmailConfirmed = true,
            FullName = _seed.AdminFullName,
            Role = ResolveAdminRole(),
            DepartmentId = null,
            Status = MemberStatus.Active,
            JoinedOn = DateOnly.FromDateTime(_clock.UtcNow),
            MustChangePassword = true
        };

        var password = string.IsNullOrWhiteSpace(_seed.AdminPassword)
            ? TemporaryPassword.Generate()
            : _seed.AdminPassword;

        var result = await _userManager.CreateAsync(admin, password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to seed OrgPresident: {errors}");
        }

        if (_environment.IsDevelopment())
        {
            // Dev convenience only. In production the temp password is emailed (Phase 2).
            _logger.LogWarning(
                "Seeded OrgPresident {Email} with temporary password: {Password}",
                _seed.AdminEmail, password);
        }
        else
        {
            _logger.LogInformation("Seeded OrgPresident {Email} (temporary password not logged outside Development).",
                _seed.AdminEmail);
        }
    }

    private SystemRole ResolveAdminRole()
    {
        if (Enum.TryParse<SystemRole>(_seed.AdminRole, ignoreCase: true, out var role))
        {
            return role;
        }

        _logger.LogWarning(
            "Seed:AdminRole '{Role}' is not a valid SystemRole; defaulting to OrgPresident.", _seed.AdminRole);
        return SystemRole.OrgPresident;
    }

    private static string DefaultName(DepartmentCode code) => code switch
    {
        DepartmentCode.PR => "Public Relations",
        DepartmentCode.Projects => "Projects",
        DepartmentCode.Social => "Social",
        DepartmentCode.Sports => "Sports",
        _ => code.ToString()
    };
}
