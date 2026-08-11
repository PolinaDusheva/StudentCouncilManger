namespace StudentCouncil.Domain.Common;

/// <summary>
/// Entities whose history must be preserved. A global query filter hides
/// rows where <see cref="IsDeleted"/> is true; the interceptor sets the flag
/// instead of issuing a physical DELETE.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAtUtc { get; set; }
}
