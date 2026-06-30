using System.Security.Cryptography;

namespace HRConnect.Application.Security;

public sealed class RefreshTokenProtector : IRefreshTokenProtector
{
    public string GenerateToken()
    {
        return Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
    }

    public string HashToken(string token)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
