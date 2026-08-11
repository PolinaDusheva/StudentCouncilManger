namespace StudentCouncil.Application.Common.Security;

/// <summary>UI-facing permission flags derived from the member's role (mirrors the auth policies).</summary>
public sealed record PermissionSet(
    bool CanManageMembers,
    bool CanManageBudget,
    bool CanManageDuties,
    bool CanCreateOrgTask,
    bool CanCreateDeptTask,
    bool CanManageEvents);

public static class MemberPermissions
{
    public static PermissionSet For(SystemRole role)
    {
        var isOrgLeadership = role is SystemRole.OrgPresident or SystemRole.OrgVicePresident;
        var canCreateDeptTask = isOrgLeadership
            || role is SystemRole.DeptPresident or SystemRole.DeptVicePresident;

        return new PermissionSet(
            CanManageMembers: isOrgLeadership,
            CanManageBudget: isOrgLeadership,
            CanManageDuties: isOrgLeadership,
            CanCreateOrgTask: isOrgLeadership,
            CanCreateDeptTask: canCreateDeptTask,
            // Every role except a plain Member can create events (functional 8.2 matrix).
            CanManageEvents: role != SystemRole.Member);
    }
}
