namespace StudentCouncil.Domain.Common;

/// <summary>
/// Base type for all persisted entities. Provides a <see cref="Guid"/> key and
/// audit fields filled by the save-changes interceptor.
/// </summary>
public abstract class BaseEntity : IAuditable
{
    public Guid Id { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? UpdatedById { get; set; }
}
