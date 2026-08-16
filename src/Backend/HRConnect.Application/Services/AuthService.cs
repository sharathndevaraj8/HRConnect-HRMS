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
    private readonly IPasswordResetRepository _passwordResetRepository;

    public AuthService(
        IUserAccountRepository userAccountRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IRefreshTokenProtector refreshTokenProtector,
        IPasswordResetRepository passwordResetRepository)
    {
        _userAccountRepository = userAccountRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _refreshTokenProtector = refreshTokenProtector;
        _passwordResetRepository = passwordResetRepository;
    }

    public async Task<UserAccount?> AuthenticateAsync(string email, string password)
    {
        var user = await _userAccountRepository.GetByEmailAsync(email);

        if (user == null || !user.IsActive || string.IsNullOrWhiteSpace(user.PasswordHash))
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

    public async Task<UserAccount> FindOrCreateExternalUserAsync(
        string provider,
        string providerSubject,
        string fullName,
        string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerSubject);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var normalizedProvider = provider.Trim().ToLowerInvariant();
        var normalizedSubject = providerSubject.Trim();
        var cleanedEmail = email.Trim().ToLowerInvariant();

        var externalUser = await _userAccountRepository.GetByExternalLoginAsync(
            normalizedProvider,
            normalizedSubject);

        if (externalUser != null)
        {
            if (!externalUser.IsActive)
            {
                throw new InvalidOperationException("This HRConnect account is inactive.");
            }

            return externalUser;
        }

        var existingUser = await _userAccountRepository.GetByEmailAsync(email);

        if (existingUser != null)
        {
            if (!existingUser.IsActive)
            {
                throw new InvalidOperationException("This HRConnect account is inactive.");
            }

            if (await _userAccountRepository.HasExternalLoginAsync(existingUser.Id, normalizedProvider))
            {
                throw new InvalidOperationException(
                    "This HRConnect account is already linked to another external account.");
            }

            await _userAccountRepository.AddExternalLoginAsync(new ExternalLogin
            {
                UserAccountId = existingUser.Id,
                Provider = normalizedProvider,
                ProviderSubject = normalizedSubject,
                ProviderEmail = cleanedEmail,
                CreatedAtUtc = DateTime.UtcNow
            });

            return existingUser;
        }

        throw new InvalidOperationException(
            "You do not have an HRConnect employee account. Please contact your administrator.");
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

    public async Task<PasswordResetIssue?> CreatePasswordResetAsync(string email, int lifetimeMinutes)
    {
        var user = await _userAccountRepository.GetByEmailAsync(email);
        if (user is not { IsActive: true }) return null;

        var now = DateTime.UtcNow;
        var rawToken = _refreshTokenProtector.GenerateToken();
        var expiresAt = now.AddMinutes(Math.Clamp(lifetimeMinutes, 5, 120));

        await _passwordResetRepository.InvalidateUnusedForUserAsync(user.Id, now);
        await _passwordResetRepository.AddAsync(new PasswordResetToken
        {
            UserAccountId = user.Id,
            TokenHash = _refreshTokenProtector.HashToken(rawToken),
            CreatedAtUtc = now,
            ExpiresAtUtc = expiresAt
        });
        await _passwordResetRepository.SaveChangesAsync();

        return new PasswordResetIssue(user.Email, user.FullName, rawToken, expiresAt);
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;

        var now = DateTime.UtcNow;
        var tokenHash = _refreshTokenProtector.HashToken(token.Trim());
        var resetToken = await _passwordResetRepository.GetActiveByHashAsync(tokenHash, now);
        if (resetToken?.UserAccount is not { IsActive: true } user) return false;

        user.PasswordHash = _passwordHasher.HashPassword(newPassword);
        resetToken.UsedAtUtc = now;
        await _passwordResetRepository.InvalidateUnusedForUserAsync(user.Id, now, resetToken.Id);
        await _passwordResetRepository.RevokeRefreshTokensForUserAsync(user.Id, now);
        await _passwordResetRepository.SaveChangesAsync();
        return true;
    }

    private RefreshTokenIssue CreateRefreshTokenIssue(int lifetimeDays)
    {
        return new RefreshTokenIssue(
            _refreshTokenProtector.GenerateToken(),
            DateTime.UtcNow.AddDays(lifetimeDays));
    }
}
