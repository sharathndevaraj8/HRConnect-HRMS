using System.ComponentModel.DataAnnotations;

namespace HRConnect.API.Options;

public sealed class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";

    [Range(5, 120)] public int TokenLifetimeMinutes { get; set; } = 30;
    [Required, Url] public string FrontendBaseUrl { get; set; } = "http://localhost:5173";
    public string SmtpHost { get; set; } = string.Empty;
    [Range(1, 65535)] public int SmtpPort { get; set; } = 587;
    public bool SmtpEnableSsl { get; set; } = true;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "HRConnect";
}
