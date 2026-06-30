using HRConnect.Domain.Entities;
using HRConnect.Application.Models;

namespace HRConnect.Application.Interfaces;

public interface IAuthService
{
    Task<UserAccount?> AuthenticateAsync(string email, string password);

    Task<UserAccount> RegisterAsync(string fullName, string email, string password, string role = "Employee");

    Task<RefreshTokenIssue> IssueRefreshTokenAsync(UserAccount user, int lifetimeDays);

    Task<RefreshTokenRotationResult?> RotateRefreshTokenAsync(string refreshToken, int lifetimeDays);

    Task RevokeRefreshTokenAsync(string refreshToken);
}
