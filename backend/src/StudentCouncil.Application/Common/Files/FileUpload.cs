namespace StudentCouncil.Application.Common.Files;

/// <summary>
/// Framework-agnostic upload payload carried by commands. The controller adapts
/// <c>IFormFile</c> into this (a seekable stream + metadata) so the Application layer
/// never references ASP.NET types (Phase 3 decision #5).
/// </summary>
public sealed class FileUpload
{
    public required Stream Content { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long Length { get; init; }
}
