using HRConnect.Application.Interfaces;
using HRConnect.Domain.Entities;
using HRConnect.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HRConnect.Infrastructure.Repositories;

public sealed class PasswordResetRepository : IPasswordResetRepository
{
    private readonly AppDbContext _context;
    public PasswordResetRepository(AppDbContext context) => _context = context;

    public Task<PasswordResetToken?> GetActiveByHashAsync(string tokenHash, DateTime nowUtc) =>
        _context.PasswordResetTokens
            .Include(x => x.UserAccount)
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash && x.UsedAtUtc == null && x.ExpiresAtUtc > nowUtc);

    public async Task AddAsync(PasswordResetToken token) =>
        await _context.PasswordResetTokens.AddAsync(token);

    public async Task InvalidateUnusedForUserAsync(int userAccountId, DateTime usedAtUtc, int? exceptTokenId = null)
    {
        var query = _context.PasswordResetTokens
            .Where(x => x.UserAccountId == userAccountId && x.UsedAtUtc == null);
        if (exceptTokenId.HasValue) query = query.Where(x => x.Id != exceptTokenId.Value);
        await query.ExecuteUpdateAsync(setters => setters.SetProperty(x => x.UsedAtUtc, usedAtUtc));
    }

    public async Task RevokeRefreshTokensForUserAsync(int userAccountId, DateTime revokedAtUtc) =>
        await _context.RefreshTokens
            .Where(x => x.UserAccountId == userAccountId && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.RevokedAtUtc, revokedAtUtc));

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
