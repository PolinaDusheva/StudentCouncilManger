namespace StudentCouncil.Application.Features.Members;

public static class RoleRules
{
    /// <summary>Org-roles have no department; Dept-roles and Member require one (spec 4.2).</summary>
    public static bool IsOrgRole(SystemRole role) =>
        role is SystemRole.OrgPresident or SystemRole.OrgVicePresident or SystemRole.OrgSecretary;
}
