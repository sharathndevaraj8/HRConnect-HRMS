using HRConnect.Domain.Entities;

namespace HRConnect.Application.Interfaces;

public interface IUserAccountRepository
{
    Task<UserAccount?> GetByEmailAsync(string email);

    Task<bool> EmailExistsAsync(string email);

    Task AddAsync(UserAccount user);
}
