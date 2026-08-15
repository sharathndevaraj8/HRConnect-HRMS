namespace HRConnect.Domain.Entities;

public sealed class PasswordResetToken
{
    public int Id { get; set; }
    public int UserAccountId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UsedAtUtc { get; set; }
    public UserAccount? UserAccount { get; set; }
}
