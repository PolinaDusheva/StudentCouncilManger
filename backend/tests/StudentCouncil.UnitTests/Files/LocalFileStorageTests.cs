using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using StudentCouncil.Infrastructure.Storage;

namespace StudentCouncil.UnitTests.Files;

public class LocalFileStorageTests
{
    private static LocalFileStorage NewStorage(string root)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.ContentRootPath.Returns(root);
        return new LocalFileStorage(Options.Create(new StorageOptions { LocalRootPath = root }), environment);
    }

    private static MemoryStream Bytes(string content) => new(Encoding.UTF8.GetBytes(content));

    private static async Task<List<string>> CollectAsync(LocalFileStorage storage, string prefix)
    {
        var keys = new List<string>();
        await foreach (var key in storage.EnumerateKeysAsync(prefix))
        {
            keys.Add(key);
        }

        return keys;
    }

    [Fact]
    public async Task EnumerateKeysAsync_returns_forward_slash_keys_scoped_to_the_prefix()
    {
        var root = Path.Combine(Path.GetTempPath(), $"sc-localstore-{Guid.NewGuid():N}");
        try
        {
            var storage = NewStorage(root);
            await storage.SaveAsync(Bytes("a"), "tasks/t1/doc1.pdf", "application/pdf");
            await storage.SaveAsync(Bytes("b"), "tasks/t2/doc2.pdf", "application/pdf");
            await storage.SaveAsync(Bytes("c"), "avatars/u1.png", "image/png");

            (await CollectAsync(storage, "tasks/")).Should().BeEquivalentTo(["tasks/t1/doc1.pdf", "tasks/t2/doc2.pdf"]);
            (await CollectAsync(storage, "avatars/")).Should().ContainSingle().Which.Should().Be("avatars/u1.png");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task EnumerateKeysAsync_on_a_missing_prefix_is_empty()
    {
        var root = Path.Combine(Path.GetTempPath(), $"sc-localstore-{Guid.NewGuid():N}");
        var storage = NewStorage(root);

        (await CollectAsync(storage, "tasks/")).Should().BeEmpty();
    }
}
