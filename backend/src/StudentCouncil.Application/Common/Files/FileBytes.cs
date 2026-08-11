namespace StudentCouncil.Application.Common.Files;

public static class FileBytes
{
    /// <summary>
    /// Reads the leading bytes of a (seekable) upload stream for signature checks, then rewinds it
    /// so the caller can stream the full content to storage. Returns fewer bytes for tiny files.
    /// </summary>
    public static async Task<byte[]> ReadHeaderAsync(Stream content, int length, CancellationToken cancellationToken)
    {
        if (!content.CanSeek)
        {
            throw new InvalidOperationException("The upload stream must be seekable for validation.");
        }

        content.Position = 0;
        var buffer = new byte[length];
        var read = await content.ReadAtLeastAsync(buffer, length, throwOnEndOfStream: false, cancellationToken);
        content.Position = 0;

        return read >= length ? buffer : buffer[..read];
    }
}
