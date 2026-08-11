using Microsoft.Extensions.Diagnostics.HealthChecks;
using StudentCouncil.Application.Abstractions;

namespace StudentCouncil.Infrastructure.Health;

/// <summary>
/// Readiness check for the file store (spec 14, decision #6): writes and immediately deletes a tiny probe
/// object through <see cref="IFileStorage"/>. For the local provider this exercises the upload directory;
/// for Azure Blob it exercises the container. A uniform write probe keeps the check provider-agnostic and
/// proves the store is actually usable, not merely reachable. The probe lives under a <c>health/</c> prefix
/// so the orphan-file cleanup job (which scans <c>tasks/</c> and <c>avatars/</c>) never touches it.
/// </summary>
public sealed class BlobStorageHealthCheck : IHealthCheck
{
    private readonly IFileStorage _storage;

    public BlobStorageHealthCheck(IFileStorage storage) => _storage = storage;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var key = $"health/probe-{Guid.NewGuid():N}";
        try
        {
            await using var content = new MemoryStream("ok"u8.ToArray());
            await _storage.SaveAsync(content, key, "application/octet-stream", cancellationToken);
            await _storage.DeleteAsync(key, cancellationToken);
            return HealthCheckResult.Healthy("File storage is writable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("File storage is not writable.", ex);
        }
    }
}
