using System.Runtime.CompilerServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StudentCouncil.Application.Abstractions;

namespace StudentCouncil.Infrastructure.Storage;

/// <summary>
/// Development <see cref="IFileStorage"/> backed by the local file system, rooted outside
/// <c>wwwroot</c>. Keys are composed by the application (never the client's file name); each
/// resolved path is verified to stay under the configured root as a defence against traversal.
/// </summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly StorageOptions _options;
    private readonly IHostEnvironment _environment;

    public LocalFileStorage(IOptions<StorageOptions> options, IHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    public async Task<string> SaveAsync(Stream content, string key, string contentType, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(file, cancellationToken);

        return key;
    }

    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(key);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The requested file does not exist.", key);
        }

        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(key);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    public Task<Uri?> GetSignedUrlAsync(string key, TimeSpan ttl, CancellationToken cancellationToken = default) =>
        Task.FromResult<Uri?>(null);

    public async IAsyncEnumerable<string> EnumerateKeysAsync(
        string prefix, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var rootFull = ResolveRoot();
        var searchRoot = Path.GetFullPath(Path.Combine(rootFull, prefix));

        // Stay under the root (the prefix is trusted, but guard defensively) and tolerate a missing folder.
        if ((!searchRoot.StartsWith(rootFull, StringComparison.Ordinal)) || !Directory.Exists(searchRoot))
        {
            yield break;
        }

        foreach (var path in Directory.EnumerateFiles(searchRoot, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Storage keys always use forward slashes, regardless of the host OS.
            yield return Path.GetRelativePath(rootFull, path).Replace(Path.DirectorySeparatorChar, '/');
        }

        await Task.CompletedTask;
    }

    /// <summary>The absolute, normalised storage root (outside <c>wwwroot</c>).</summary>
    private string ResolveRoot()
    {
        var root = Path.IsPathRooted(_options.LocalRootPath)
            ? _options.LocalRootPath
            : Path.Combine(_environment.ContentRootPath, _options.LocalRootPath);
        return Path.GetFullPath(root);
    }

    /// <summary>Maps a storage key to an absolute path and guarantees it stays under the root.</summary>
    private string ResolvePath(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("The storage key must not be empty.", nameof(key));
        }

        var rootFull = ResolveRoot();

        var combined = Path.GetFullPath(Path.Combine(rootFull, key));
        if (!combined.StartsWith(rootFull + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !string.Equals(combined, rootFull, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("The storage key resolves outside the storage root.");
        }

        return combined;
    }
}
