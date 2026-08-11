namespace StudentCouncil.Infrastructure.Storage;

/// <summary>Binds the <c>Storage</c> configuration section (spec 8 / 15).</summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary><c>Local</c> (dev) or <c>AzureBlob</c> (production). Selected by the DI switch (spec 8).</summary>
    public string Provider { get; set; } = "Local";

    /// <summary>Azure Blob connection string (env/Key Vault in production); required when <see cref="Provider"/> is <c>AzureBlob</c>.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>Root directory for the local provider — kept outside <c>wwwroot</c>.
    /// Relative paths are resolved against the content root.</summary>
    public string LocalRootPath { get; set; } = "App_Data/uploads";

    /// <summary>Container/prefix for task documents.</summary>
    public string TasksContainer { get; set; } = "task-documents";

    /// <summary>Container/prefix for profile avatars.</summary>
    public string AvatarsContainer { get; set; } = "avatars";

    /// <summary>Maximum accepted upload size for task documents, in megabytes (spec 8: 25 MB).</summary>
    public int MaxFileSizeMb { get; set; } = 25;

    /// <summary>Maximum accepted upload size for avatars, in megabytes (smaller than documents).</summary>
    public int MaxAvatarSizeMb { get; set; } = 5;

    public long MaxFileSizeBytes => MaxFileSizeMb * 1024L * 1024L;

    public long MaxAvatarSizeBytes => MaxAvatarSizeMb * 1024L * 1024L;
}
