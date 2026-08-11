namespace StudentCouncil.Application.Common.Audit;

/// <summary>Stable entity-type names written to <c>audit_logs.entity_type</c> (spec 14). "Member" maps to the
/// Identity <c>ApplicationUser</c>, which the Application layer never references directly.</summary>
public static class AuditEntities
{
    public const string Member = "Member";
    public const string TaskItem = "TaskItem";
    public const string TaskDocument = "TaskDocument";
    public const string DutyRecord = "DutyRecord";
    public const string Expense = "Expense";
}
