namespace StudentCouncil.Application.Common.Options;

/// <summary>Password-reset link settings. The reset form itself is outside the backend's scope.</summary>
public sealed class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";

    public int LinkValidMinutes { get; set; } = 30;

    /// <summary>Base URL of the (web-rendered) reset form the emailed link points to.</summary>
    public string ResetUrlBase { get; set; } = "https://app.studentcouncil.ue-varna.bg/reset-password";
}
