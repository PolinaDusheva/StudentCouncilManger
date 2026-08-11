using StudentCouncil.Application.Common.Files;

namespace StudentCouncil.Api.Controllers;

/// <summary>
/// Adapts ASP.NET's <see cref="IFormFile"/> into the framework-agnostic <see cref="FileUpload"/>
/// the Application layer expects (a seekable stream + metadata), keeping ASP.NET types out of
/// the use-case handlers (Phase 3 decision #5).
/// </summary>
public static class FormFileExtensions
{
    public static async Task<FileUpload> ToUploadAsync(this IFormFile file, CancellationToken cancellationToken)
    {
        // Buffer into a seekable stream so the handler can read the header (magic bytes) and rewind
        // before streaming the full content to storage.
        var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        return new FileUpload
        {
            Content = buffer,
            FileName = file.FileName,
            ContentType = file.ContentType,
            Length = file.Length
        };
    }
}
