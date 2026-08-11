namespace StudentCouncil.Domain.Common;

/// <summary>
/// Marker + contract for entities whose audit fields are populated automatically
/// by the EF Core save-changes interceptor.
/// </summary>
public interface IAuditable
{
    DateTime CreatedAtUtc { get; set; }
    Guid? CreatedById { get; set; }
    DateTime? UpdatedAtUtc { get; set; }
    Guid? UpdatedById { get; set; }
}
