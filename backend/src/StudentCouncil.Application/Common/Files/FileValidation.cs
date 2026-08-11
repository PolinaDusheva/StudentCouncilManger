using StudentCouncil.Application.Common.Exceptions;

namespace StudentCouncil.Application.Common.Files;

/// <summary>
/// Enforces the upload rules from spec 8 / 13: size limit, an extension on the whitelist,
/// a declared MIME consistent with that extension, and magic bytes that match the claimed
/// format. The declared content type is trusted only when it is specific — clients commonly
/// send <c>application/octet-stream</c>, in which case the signature check carries the weight.
/// </summary>
public static class FileValidation
{
    /// <summary>
    /// Validates the metadata + leading bytes of an upload. Throws
    /// <see cref="FileTooLargeException"/> (413) when oversized and
    /// <see cref="BadRequestException"/> (400, <c>unsupported_file_type</c>) otherwise.
    /// Returns the normalised (lower-case) extension, useful for composing the storage key.
    /// </summary>
    public static string Validate(
        string fileName,
        string? contentType,
        long length,
        ReadOnlySpan<byte> header,
        long maxSizeBytes,
        IReadOnlyDictionary<string, string[]> allowed)
    {
        if (length <= 0)
        {
            throw new BadRequestException("The uploaded file is empty.", "unsupported_file_type");
        }

        if (length > maxSizeBytes)
        {
            throw new FileTooLargeException(
                $"The uploaded file exceeds the maximum allowed size of {maxSizeBytes / (1024 * 1024)} MB.");
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !allowed.TryGetValue(extension, out var allowedMimes))
        {
            throw new BadRequestException("This file type is not allowed.", "unsupported_file_type");
        }

        // A specific declared content type must be one permitted for the extension.
        if (!string.IsNullOrWhiteSpace(contentType) && !IsGeneric(contentType))
        {
            var declared = contentType.Split(';')[0].Trim();
            if (!allowedMimes.Contains(declared, StringComparer.OrdinalIgnoreCase))
            {
                throw new BadRequestException("The file's content type does not match its extension.", "unsupported_file_type");
            }
        }

        if (!FileSignatures.Matches(extension, header))
        {
            throw new BadRequestException("The file's content does not match its extension.", "unsupported_file_type");
        }

        return extension.ToLowerInvariant();
    }

    private static bool IsGeneric(string contentType) =>
        contentType.StartsWith("application/octet-stream", StringComparison.OrdinalIgnoreCase);
}
