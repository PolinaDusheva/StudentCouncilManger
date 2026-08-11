namespace StudentCouncil.Application.Abstractions;

/// <summary>
/// Read-only projection of a member, safe to expose to the Application layer for
/// querying (filtering/sorting/paging) without leaking the Identity entity.
/// </summary>
public sealed class MemberView
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? PhotoUrl { get; init; }
    public SystemRole Role { get; init; }
    public Guid? DepartmentId { get; init; }
    public DepartmentCode? DepartmentCode { get; init; }
    public string? DepartmentName { get; init; }
    public DateOnly JoinedOn { get; init; }
    public MemberStatus Status { get; init; }
}

/// <summary>
/// Composable read access to members. The returned <see cref="IQueryable{T}"/> is backed by
/// EF Core, so callers can apply <c>Where</c>/<c>OrderBy</c>/<c>Skip</c>/<c>Take</c> and execute
/// asynchronously. Filters compose over direct columns to stay translatable.
/// </summary>
public interface IMemberDirectory
{
    IQueryable<MemberView> Members { get; }
}
