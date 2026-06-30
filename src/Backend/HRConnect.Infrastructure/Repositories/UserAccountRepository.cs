using HRConnect.Application.Interfaces;
using HRConnect.Domain.Entities;
using HRConnect.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HRConnect.Infrastructure.Repositories;

public sealed class UserAccountRepository : IUserAccountRepository
{
    private readonly AppDbContext _context;

    public UserAccountRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserAccount?> GetByEmailAsync(string email)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();

        return await _context.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();

        return await _context.UserAccounts
            .AnyAsync(user => user.NormalizedEmail == normalizedEmail);
    }

    public async Task AddAsync(UserAccount user)
    {
        await _context.UserAccounts.AddAsync(user);
        await _context.SaveChangesAsync();
    }
}
