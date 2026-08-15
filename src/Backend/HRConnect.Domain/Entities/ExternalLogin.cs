namespace HRConnect.Domain.Entities;

public sealed class ExternalLogin
{
    public int Id { get; set; }

    public int UserAccountId { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string ProviderSubject { get; set; } = string.Empty;

    public string ProviderEmail { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public UserAccount UserAccount { get; set; } = null!;
}
