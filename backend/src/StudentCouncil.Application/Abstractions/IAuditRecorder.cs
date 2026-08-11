namespace StudentCouncil.Application.Abstractions;

/// <summary>
/// The single handler-facing seam for the audit trail (spec 14). <see cref="Record"/> stages an
/// <see cref="Domain.Entities.AuditLog"/> row in the current unit of work; it is committed by the
/// handler's own <c>SaveChangesAsync</c>, so the audit entry and the business change succeed or fail
/// together (decision #1). It never calls <c>SaveChanges</c> itself.
/// </summary>
public interface IAuditRecorder
{
    /// <summary>Stages an audit entry for <paramref name="action"/> on the given entity. Does not save.</summary>
    void Record(string action, string entityType, string entityId, object? details = null);

    /// <summary>Convenience overload for the common case of a <see cref="Guid"/> entity id.</summary>
    void Record(string action, string entityType, Guid entityId, object? details = null) =>
        Record(action, entityType, entityId.ToString(), details);
}
