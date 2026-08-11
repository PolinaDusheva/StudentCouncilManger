namespace StudentCouncil.Application.Common.Files;

/// <summary>
/// A readable file stream plus the metadata a controller needs to serve it. When
/// <see cref="FileName"/> is set the controller sends it as an attachment; otherwise inline.
/// </summary>
public sealed record StreamedFile(Stream Content, string ContentType, string? FileName = null);
