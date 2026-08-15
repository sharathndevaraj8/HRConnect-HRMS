namespace HRConnect.Domain.Entities;

public class UserAccount
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? PasswordHash { get; set; }

    public string Role { get; set; } = "Employee";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public int? EmployeeId { get; set; }

    public ICollection<ExternalLogin> ExternalLogins { get; set; } = [];

    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = [];

    public Employee? Employee { get; set; }
}
