using System.Net;
using System.Net.Mail;
using HRConnect.API.Options;
using HRConnect.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace HRConnect.API.Services;

public sealed class SmtpPasswordResetEmailSender : IPasswordResetEmailSender
{
    private readonly PasswordResetOptions _options;
    private readonly ILogger<SmtpPasswordResetEmailSender> _logger;

    public SmtpPasswordResetEmailSender(
        IOptions<PasswordResetOptions> options,
        ILogger<SmtpPasswordResetEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendAsync(
        string email,
        string fullName,
        string resetUrl,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SmtpHost) || string.IsNullOrWhiteSpace(_options.FromEmail))
            return false;

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromEmail, _options.FromName),
                Subject = "Reset your HRConnect password",
                IsBodyHtml = true,
                Body = $"""
                    <p>Hello {WebUtility.HtmlEncode(fullName)},</p>
                    <p>Use the secure link below to reset your HRConnect password. It expires at {expiresAtUtc:u}.</p>
                    <p><a href="{WebUtility.HtmlEncode(resetUrl)}">Reset HRConnect password</a></p>
                    <p>If you did not request this, you can ignore this email.</p>
                    """
            };
            message.To.Add(email);

            using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
            {
                EnableSsl = _options.SmtpEnableSsl,
                UseDefaultCredentials = string.IsNullOrWhiteSpace(_options.SmtpUsername),
                Credentials = string.IsNullOrWhiteSpace(_options.SmtpUsername)
                    ? CredentialCache.DefaultNetworkCredentials
                    : new NetworkCredential(_options.SmtpUsername, _options.SmtpPassword)
            };
            cancellationToken.ThrowIfCancellationRequested();
            await client.SendMailAsync(message, cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception is SmtpException or InvalidOperationException or FormatException)
        {
            _logger.LogError(exception, "Unable to deliver a password-reset email.");
            return false;
        }
    }
}
