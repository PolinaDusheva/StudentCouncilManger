namespace StudentCouncil.Application.Abstractions;

/// <summary>
/// Abstraction over the blob/file store (spec 8). Keys are composed by the callers
/// (e.g. <c>tasks/{taskId}/{guid}{ext}</c>), never the client's original file name.
/// The local dev provider streams through an API endpoint; a future Azure Blob provider
/// can return short-lived signed URLs from <see cref="GetSignedUrlAsync"/>.
/// </summary>
public interface IFileStorage
{
    /// <summary>Persists <paramref name="content"/> under <paramref name="key"/> and returns the stored key.</summary>
    Task<string> SaveAsync(Stream content, string key, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Opens a readable stream for a previously stored key.</summary>
    Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Deletes the object at <paramref name="key"/>; succeeds silently if it does not exist.</summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Returns a short-lived signed URL for direct download, or <c>null</c> when the
    /// provider has no such concept (the local provider streams through the API instead).</summary>
    Task<Uri?> GetSignedUrlAsync(string key, TimeSpan ttl, CancellationToken cancellationToken = default);

    /// <summary>Streams the keys of every stored object whose key starts with <paramref name="prefix"/>
    /// (e.g. <c>"tasks/"</c> or <c>"avatars/"</c>). Used by the orphan-file cleanup job (spec 10).</summary>
    IAsyncEnumerable<string> EnumerateKeysAsync(string prefix, CancellationToken cancellationToken = default);
}
