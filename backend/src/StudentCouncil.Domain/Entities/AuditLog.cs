namespace StudentCouncil.Domain.Entities;

/// <summary>Traceability record for sensitive actions (role change, deletion, expense, duty).</summary>
public class AuditLog
{
    public Guid Id { get; set; }

    /// <summary>FK -> Member (ApplicationUser) who performed the action.</summary>
    public Guid ActorId { get; set; }

    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }

    /// <summary>JSON details of the change.</summary>
    public string? Details { get; set; }
}
