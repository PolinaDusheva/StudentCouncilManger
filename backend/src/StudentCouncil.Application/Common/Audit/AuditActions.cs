namespace StudentCouncil.Application.Common.Audit;

/// <summary>Stable action names written to <c>audit_logs.action</c> (spec 14). Kept as constants so the
/// handlers and the audit-trail tests agree on a single source of truth.</summary>
public static class AuditActions
{
    // Members (spec 14: create / deactivate / role change).
    public const string MemberCreated = "MemberCreated";
    public const string MemberUpdated = "MemberUpdated";
    public const string RoleChanged = "RoleChanged";
    public const string MemberDeactivated = "MemberDeactivated";
    public const string MemberReactivated = "MemberReactivated";

    // Tasks / documents.
    public const string TaskDeleted = "TaskDeleted";
    public const string DocumentDeleted = "DocumentDeleted";

    // Duties.
    public const string DutyRegistered = "DutyRegistered";
    public const string DutyUpdated = "DutyUpdated";
    public const string DutyDeleted = "DutyDeleted";

    // Budget.
    public const string ExpenseAdded = "ExpenseAdded";
    public const string ExpenseUpdated = "ExpenseUpdated";
    public const string ExpenseDeleted = "ExpenseDeleted";
}
