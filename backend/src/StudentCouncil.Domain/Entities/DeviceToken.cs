namespace StudentCouncil.Domain.Entities;

public class DeviceToken
{
    public Guid Id { get; set; }

    /// <summary>FK -> Member (ApplicationUser).</summary>
    public Guid MemberId { get; set; }

    /// <summary>FCM / APNs token.</summary>
    public string Token { get; set; } = string.Empty;
    public DevicePlatform Platform { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastSeenUtc { get; set; }
}
