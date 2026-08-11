using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using StudentCouncil.Application.Abstractions;
using StudentCouncil.Domain.Entities;

namespace StudentCouncil.Infrastructure.Audit;

/// <summary>
/// Stages <see cref="AuditLog"/> rows in the current <see cref="IAppDbContext"/> change-tracker so they
/// commit atomically with the handler's business change (decision #1). The actor is the authenticated
/// caller (decision #2); a missing actor is unexpected, so the entry is skipped with a warning rather than
/// throwing and aborting the business action.
/// </summary>
public sealed class AuditRecorder : IAuditRecorder
{
    // Camel-case property names + string enums so the jsonb payload matches the rest of the JSON API
    // and stays human-readable for investigation.
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;
    private readonly ILogger<AuditRecorder> _logger;

    public AuditRecorder(IAppDbContext db, ICurrentUser currentUser, IDateTime clock, ILogger<AuditRecorder> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _logger = logger;
    }

    public void Record(string action, string entityType, string entityId, object? details = null)
    {
        if (_currentUser.Id is not { } actorId)
        {
            _logger.LogWarning(
                "Skipping audit entry {Action} on {EntityType} {EntityId}: no authenticated actor.",
                action, entityType, entityId);
            return;
        }

        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Timestamp = _clock.UtcNow,
            Details = details is null ? null : JsonSerializer.Serialize(details, SerializerOptions)
        });
    }
}
