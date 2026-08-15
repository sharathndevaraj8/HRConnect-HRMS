using HRConnect.Domain.Entities;

namespace HRConnect.Application.Interfaces;

public interface IPasswordResetRepository
{
    Task<PasswordResetToken?> GetActiveByHashAsync(string tokenHash, DateTime nowUtc);
    Task AddAsync(PasswordResetToken token);
    Task InvalidateUnusedForUserAsync(int userAccountId, DateTime usedAtUtc, int? exceptTokenId = null);
    Task RevokeRefreshTokensForUserAsync(int userAccountId, DateTime revokedAtUtc);
    Task SaveChangesAsync();
}
