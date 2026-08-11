using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Domain.Enums;
using StudentCouncil.Infrastructure.Persistence;

namespace StudentCouncil.IntegrationTests;

/// <summary>One active member per role (token + id) plus the two departments used for scope negatives.</summary>
public sealed record SeededRoles(
    IReadOnlyDictionary<string, string> TokensByRole,
    IReadOnlyDictionary<string, Guid> MemberIdsByRole,
    Guid PrimaryDepartmentId,
    Guid OtherDepartmentId);

/// <summary>
/// Seeds the full role set for the authorization matrix (decision #11). Members are created and tokens minted
/// directly through the DI container — no HTTP login round-trips — so the matrix never touches the auth rate
/// limit (plan §10) and needs no email. Minted tokens carry the member's real security stamp and clear the
/// password gate, so they authenticate exactly like a real login.
/// </summary>
public static class RoleSeeding
{
    public const string Member = "Member";
    public const string DeptSecretary = "DeptSecretary";
    public const string DeptVicePresident = "DeptVicePresident";
    public const string DeptPresident = "DeptPresident";
    public const string OrgSecretary = "OrgSecretary";
    public const string OrgVicePresident = "OrgVicePresident";
    public const string OrgPresident = "OrgPresident";

    public static readonly IReadOnlyList<string> AllRoles =
        [Member, DeptSecretary, DeptVicePresident, DeptPresident, OrgSecretary, OrgVicePresident, OrgPresident];

    public static async Task<SeededRoles> SeedAsync(IServiceProvider rootServices)
    {
        using var scope = rootServices.CreateScope();
        var services = scope.ServiceProvider;
        var identity = services.GetRequiredService<IIdentityService>();
        var jwt = services.GetRequiredService<IJwtTokenService>();
        var db = services.GetRequiredService<AppDbContext>();

        var primaryId = await db.Departments.Where(d => d.Code == DepartmentCode.PR).Select(d => d.Id).SingleAsync();
        var otherId = await db.Departments.Where(d => d.Code == DepartmentCode.Social).Select(d => d.Id).SingleAsync();

        var tokens = new Dictionary<string, string>(StringComparer.Ordinal);
        var ids = new Dictionary<string, Guid>(StringComparer.Ordinal);

        // The OrgPresident is the admin seeded at application startup.
        var admin = await identity.FindByEmailAsync(SmokeTestFactory.AdminEmail)
            ?? throw new InvalidOperationException("The seeded admin account was not found.");
        tokens[OrgPresident] = Mint(jwt, admin);
        ids[OrgPresident] = admin.Id;

        foreach (var role in AllRoles.Where(r => r != OrgPresident))
        {
            var systemRole = Enum.Parse<SystemRole>(role);
            var departmentId = role.StartsWith("Org", StringComparison.Ordinal) ? (Guid?)null : primaryId;

            var result = await identity.CreateMemberAsync(new NewMember(
                $"Matrix {role}", $"matrix.{role.ToLowerInvariant()}@ue-varna.bg", null,
                systemRole, departmentId, new DateOnly(2026, 2, 1)));
            if (!result.Succeeded || result.Value is null)
            {
                throw new InvalidOperationException($"Could not seed the {role} member: {result.Error}");
            }

            var account = await identity.FindByIdAsync(result.Value.Id)
                ?? throw new InvalidOperationException($"The seeded {role} member could not be loaded.");
            tokens[role] = Mint(jwt, account);
            ids[role] = account.Id;
        }

        return new SeededRoles(tokens, ids, primaryId, otherId);
    }

    private static string Mint(IJwtTokenService jwt, MemberAccount account) =>
        jwt.CreateAccessToken(new TokenUser(
            account.Id, account.Email, account.FullName, account.Role,
            account.DepartmentId, account.DepartmentCode, account.SecurityStamp, MustChangePassword: false)).Token;
}
