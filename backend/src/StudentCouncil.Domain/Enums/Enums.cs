namespace StudentCouncil.Domain.Enums;

/// <summary>
/// Single source of truth for authorization. Written into the JWT `role` claim.
/// Org-roles have no <c>DepartmentId</c>; Dept-roles and Member require one.
/// </summary>
public enum SystemRole
{
    Member,
    DeptSecretary,
    DeptVicePresident,
    DeptPresident,
    OrgSecretary,
    OrgVicePresident,
    OrgPresident
}

public enum DepartmentCode
{
    PR,
    Projects,
    Social,
    Sports
}

public enum MemberStatus
{
    Active,
    Inactive
}

public enum TaskScope
{
    Organizational,
    Departmental
}

public enum TaskPriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum TaskStatus
{
    New,
    InProgress,
    InReview,
    Completed,
    Cancelled
}

public enum EventType
{
    Meeting,
    PublicEvent,
    InternalMeeting,
    SportsEvent,
    Deadline,
    Other
}

public enum RecurrenceType
{
    None,
    Weekly,
    Monthly
}

public enum DevicePlatform
{
    Android,
    iOS
}

public enum NotificationType
{
    TaskAssigned,
    TaskDueSoon,
    TaskOverdue,
    TaskStatusChanged,
    TaskComment,
    EventCreated,
    EventReminder,
    EventChanged,
    DutyReminder
}
