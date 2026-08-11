namespace StudentCouncil.Application.Common.Files;

/// <summary>
/// Upload size limits surfaced to the Application layer, so handlers can enforce them without
/// referencing Infrastructure's <c>StorageOptions</c>. Infrastructure registers this from config.
/// </summary>
public sealed record StorageLimits(long MaxDocumentBytes, long MaxAvatarBytes);
