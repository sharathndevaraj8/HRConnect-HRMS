using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace HRConnect.Application.Security;

public sealed class Argon2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;

    // Argon2id parameters
    private const int MemorySize = 65_536; // 64 MB
    private const int Iterations = 3;
    private const int Parallelism = 1;

    private const string Version = "argon2id-v1";

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = GenerateKey(password, salt, MemorySize, Iterations, Parallelism);

        return string.Join(
            '.',
            Version,
            $"m{MemorySize}",
            $"t{Iterations}",
            $"p{Parallelism}",
            Convert.ToBase64String(salt),
            Convert.ToBase64String(key));
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        var parts = passwordHash.Split('.');

        if (parts.Length != 6 || parts[0] != Version)
        {
            return false;
        }

        if (!int.TryParse(parts[1].TrimStart('m'), out var memorySize) ||
            !int.TryParse(parts[2].TrimStart('t'), out var iterations) ||
            !int.TryParse(parts[3].TrimStart('p'), out var parallelism))
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[4]);
            var expectedKey = Convert.FromBase64String(parts[5]);

            var actualKey = GenerateKey(
                password,
                salt,
                memorySize,
                iterations,
                parallelism,
                expectedKey.Length);

            return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static byte[] GenerateKey(
        string password,
        byte[] salt,
        int memorySize,
        int iterations,
        int parallelism,
        int keySize = KeySize)
    {
        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = memorySize,
            Iterations = iterations,
            DegreeOfParallelism = parallelism
        };

        return argon2.GetBytes(keySize);
    }
}