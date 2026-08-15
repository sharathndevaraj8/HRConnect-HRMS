namespace HRConnect.Application.Interfaces;

public interface IPasswordResetEmailSender
{
    Task<bool> SendAsync(
        string email,
        string fullName,
        string resetUrl,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default);
}
