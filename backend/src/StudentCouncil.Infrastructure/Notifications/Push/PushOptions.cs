namespace StudentCouncil.Infrastructure.Notifications.Push;

/// <summary>Push provider credentials (spec 15). Real values come from env/Key Vault in production.</summary>
public sealed class PushOptions
{
    public const string SectionName = "Push";

    public FcmOptions Fcm { get; set; } = new();
    public ApnsOptions Apns { get; set; } = new();
}

public sealed class FcmOptions
{
    /// <summary>Firebase service-account JSON (the FCM admin credential).</summary>
    public string? ServiceAccountJson { get; set; }
}

public sealed class ApnsOptions
{
    public string? KeyId { get; set; }
    public string? TeamId { get; set; }
    public string? BundleId { get; set; }

    /// <summary>The APNs auth key (.p8) contents.</summary>
    public string? P8Key { get; set; }
}
