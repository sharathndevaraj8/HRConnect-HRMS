using HRConnect.Domain.Entities;

namespace HRConnect.Application.Interfaces;

public interface IUserAccountRepository
{
    Task<UserAccount?> GetByEmailAsync(string email);

    Task<UserAccount?> GetByExternalLoginAsync(string provider, string providerSubject);

    Task<bool> EmailExistsAsync(string email);

    Task<bool> HasExternalLoginAsync(int userAccountId, string provider);

    Task AddAsync(UserAccount user);

    Task AddExternalLoginAsync(ExternalLogin externalLogin);

    Task AddExternalUserAsync(UserAccount user, ExternalLogin externalLogin);
}
