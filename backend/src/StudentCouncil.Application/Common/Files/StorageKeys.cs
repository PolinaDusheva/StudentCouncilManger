namespace StudentCouncil.Application.Common.Files;

/// <summary>
/// Composes blob/file keys (spec 8). Keys are derived from our own ids — never the client's
/// file name — to defend against path traversal and collisions.
/// </summary>
public static class StorageKeys
{
    /// <summary><c>tasks/{taskId}/{guid}{ext}</c> — a fresh key per uploaded document.</summary>
    public static string TaskDocument(Guid taskId, string extension) =>
        $"tasks/{taskId:N}/{Guid.NewGuid():N}{extension}";

    /// <summary><c>avatars/{memberId}{ext}</c> — deterministic so a new photo overwrites the old.</summary>
    public static string Avatar(Guid memberId, string extension) =>
        $"avatars/{memberId:N}{extension}";
}
