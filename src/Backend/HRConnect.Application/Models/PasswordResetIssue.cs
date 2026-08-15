namespace HRConnect.Application.Models;

public sealed record PasswordResetIssue(
    string Email,
    string FullName,
    string Token,
    DateTime ExpiresAtUtc);
