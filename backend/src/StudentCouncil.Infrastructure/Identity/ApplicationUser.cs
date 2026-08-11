using Microsoft.AspNetCore.Identity;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Infrastructure.Identity;

/// <summary>
/// The Member entity. Lives in Infrastructure because it derives from
/// <see cref="IdentityUser{TKey}"/>; Domain references it only by Guid FK.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }

    /// <summary>Authorization role; written into the JWT.</summary>
    public SystemRole Role { get; set; }

    /// <summary>Null for Org-roles; required for Dept-roles and Member.</summary>
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public DateOnly JoinedOn { get; set; }
    public MemberStatus Status { get; set; } = MemberStatus.Active;

    /// <summary>True on creation/reset until the member sets their own password.</summary>
    public bool MustChangePassword { get; set; }
}
