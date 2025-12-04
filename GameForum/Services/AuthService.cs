using System.Security.Cryptography;
using System.Text;

namespace GameForum.Services;

public class AuthService
{
    public void CreatePasswordHash(string password, out byte[] hash, out byte[] salt)
    {
        using var rng = RandomNumberGenerator.Create();
        salt = new byte[16];
        rng.GetBytes(salt);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        hash = pbkdf2.GetBytes(32);
    }

    public bool VerifyPassword(string password, byte[] hash, byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var check = pbkdf2.GetBytes(32);
        return CryptographicOperations.FixedTimeEquals(check, hash);
    }
}

