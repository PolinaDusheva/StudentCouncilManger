using StudentCouncil.Domain.Common;

namespace StudentCouncil.Domain.Entities;

public class TaskDocument : BaseEntity
{
    public Guid TaskItemId { get; set; }
    public TaskItem TaskItem { get; set; } = null!;

    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>Blob path / key. Never returned to the client directly.</summary>
    public string StorageKey { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }

    /// <summary>FK -> Member (ApplicationUser).</summary>
    public Guid UploadedById { get; set; }
    public DateTime UploadedAtUtc { get; set; }
}
