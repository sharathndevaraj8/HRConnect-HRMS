using HRConnect.Domain.Entities;

namespace HRConnect.API.Security;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateAccessToken(UserAccount user);
}
