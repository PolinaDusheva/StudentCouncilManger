namespace StudentCouncil.Infrastructure.Notifications.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary><c>Smtp</c> or <c>Logging</c>. Selection is environment-driven; this documents intent.</summary>
    public string Provider { get; set; } = "Logging";

    public string From { get; set; } = "no-reply@studentcouncil.ue-varna.bg";

    public SmtpOptions Smtp { get; set; } = new();
}

public sealed class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool UseStartTls { get; set; } = true;
}
