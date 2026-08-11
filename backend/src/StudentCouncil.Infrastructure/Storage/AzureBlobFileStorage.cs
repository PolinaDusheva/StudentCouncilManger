using System.Runtime.CompilerServices;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using StudentCouncil.Application.Abstractions;

namespace StudentCouncil.Infrastructure.Storage;

/// <summary>
/// Production <see cref="IFileStorage"/> backed by a private Azure Blob container (spec 8). One container
/// holds every object, namespaced by key prefix (<c>tasks/</c>, <c>avatars/</c>), mirroring the local
/// provider's single-root layout. The container is private; downloads go through the API or a short-lived
/// SAS URL from <see cref="GetSignedUrlAsync"/> (5 minutes by default).
/// </summary>
public sealed class AzureBlobFileStorage : IFileStorage
{
    private static readonly TimeSpan DefaultSignedUrlTtl = TimeSpan.FromMinutes(5);

    private readonly BlobContainerClient _container;

    public AzureBlobFileStorage(IOptions<StorageOptions> options)
    {
        var config = options.Value;
        if (string.IsNullOrWhiteSpace(config.ConnectionString))
        {
            throw new InvalidOperationException("Storage:ConnectionString is required for the Azure Blob provider.");
        }

        _container = new BlobContainerClient(config.ConnectionString, config.TasksContainer);
    }

    public async Task<string> SaveAsync(Stream content, string key, string contentType, CancellationToken cancellationToken = default)
    {
        await _container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blob = _container.GetBlobClient(key);
        await blob.UploadAsync(
            content,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken);

        return key;
    }

    public async Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        var blob = _container.GetBlobClient(key);
        try
        {
            return await blob.OpenReadAsync(cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            throw new FileNotFoundException("The requested file does not exist.", key);
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var blob = _container.GetBlobClient(key);
        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public Task<Uri?> GetSignedUrlAsync(string key, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var blob = _container.GetBlobClient(key);
        if (!blob.CanGenerateSasUri)
        {
            return Task.FromResult<Uri?>(null);
        }

        var lifetime = ttl <= TimeSpan.Zero ? DefaultSignedUrlTtl : ttl;
        var builder = new BlobSasBuilder(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(lifetime))
        {
            BlobContainerName = _container.Name,
            BlobName = key,
            Resource = "b"
        };

        return Task.FromResult<Uri?>(blob.GenerateSasUri(builder));
    }

    public async IAsyncEnumerable<string> EnumerateKeysAsync(
        string prefix, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var blobs = _container.GetBlobsAsync(
            BlobTraits.None, BlobStates.None, prefix, cancellationToken);
        await foreach (var blob in blobs)
        {
            yield return blob.Name;
        }
    }
}
