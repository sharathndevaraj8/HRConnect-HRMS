using HRConnect.Application.Interfaces;
using HRConnect.Application.Models;
using HRConnect.Application.Security;
using HRConnect.Domain.Entities;

namespace HRConnect.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenProtector _refreshTokenProtector;

    public AuthService(
        IUserAccountRepository userAccountRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IRefreshTokenProtector refreshTokenProtector)
    {
        _userAccountRepository = userAccountRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _refreshTokenProtector = refreshTokenProtector;
    }

    public async Task<UserAccount?> AuthenticateAsync(string email, string password)
    {
        var user = await _userAccountRepository.GetByEmailAsync(email);

        if (user == null || !user.IsActive)
        {
            return null;
        }

        return _passwordHasher.VerifyPassword(password, user.PasswordHash)
            ? user
            : null;
    }

    public async Task<UserAccount> RegisterAsync(
        string fullName,
        string email,
        string password,
        string role = "Employee")
    {
        if (await _userAccountRepository.EmailExistsAsync(email))
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        var normalizedEmail = email.Trim().ToUpperInvariant();
        var user = new UserAccount
        {
            FullName = string.Join(' ', fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)),
            Email = email.Trim().ToLowerInvariant(),
            NormalizedEmail = normalizedEmail,
            PasswordHash = _passwordHasher.HashPassword(password),
            Role = string.IsNullOrWhiteSpace(role) ? "Employee" : role.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _userAccountRepository.AddAsync(user);
        return user;
    }

    public async Task<RefreshTokenIssue> IssueRefreshTokenAsync(UserAccount user, int lifetimeDays)
    {
        var issue = CreateRefreshTokenIssue(lifetimeDays);

        await _refreshTokenRepository.AddAsync(new RefreshToken
        {
            UserAccountId = user.Id,
            TokenHash = _refreshTokenProtector.HashToken(issue.Token),
            ExpiresAtUtc = issue.ExpiresAtUtc,
            CreatedAtUtc = DateTime.UtcNow
        });
        await _refreshTokenRepository.SaveChangesAsync();

        return issue;
    }

    public async Task<RefreshTokenRotationResult?> RotateRefreshTokenAsync(string refreshToken, int lifetimeDays)
    {
        var tokenHash = _refreshTokenProtector.HashToken(refreshToken);
        var existingRefreshToken = await _refreshTokenRepository.GetByHashAsync(tokenHash);

        if (existingRefreshToken?.UserAccount == null ||
            !existingRefreshToken.UserAccount.IsActive ||
            !existingRefreshToken.IsActive)
        {
            return null;
        }

        var replacementIssue = CreateRefreshTokenIssue(lifetimeDays);
        var replacementHash = _refreshTokenProtector.HashToken(replacementIssue.Token);

        existingRefreshToken.RevokedAtUtc = DateTime.UtcNow;
        existingRefreshToken.ReplacedByTokenHash = replacementHash;

        await _refreshTokenRepository.AddAsync(new RefreshToken
        {
            UserAccountId = existingRefreshToken.UserAccountId,
            TokenHash = replacementHash,
            ExpiresAtUtc = replacementIssue.ExpiresAtUtc,
            CreatedAtUtc = DateTime.UtcNow
        });
        await _refreshTokenRepository.SaveChangesAsync();

        return new RefreshTokenRotationResult(existingRefreshToken.UserAccount, replacementIssue);
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        var tokenHash = _refreshTokenProtector.HashToken(refreshToken);
        var existingRefreshToken = await _refreshTokenRepository.GetByHashAsync(tokenHash);

        if (existingRefreshToken is { RevokedAtUtc: null })
        {
            existingRefreshToken.RevokedAtUtc = DateTime.UtcNow;
            await _refreshTokenRepository.SaveChangesAsync();
        }
    }

    private RefreshTokenIssue CreateRefreshTokenIssue(int lifetimeDays)
    {
        return new RefreshTokenIssue(
            _refreshTokenProtector.GenerateToken(),
            DateTime.UtcNow.AddDays(lifetimeDays));
    }
}
