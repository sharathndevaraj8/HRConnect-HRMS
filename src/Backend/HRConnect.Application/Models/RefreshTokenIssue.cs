namespace HRConnect.Application.Models;

public sealed record RefreshTokenIssue(string Token, DateTime ExpiresAtUtc);
