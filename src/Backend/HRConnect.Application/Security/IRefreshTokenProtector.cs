namespace HRConnect.Application.Security;

public interface IRefreshTokenProtector
{
    string GenerateToken();

    string HashToken(string token);
}
