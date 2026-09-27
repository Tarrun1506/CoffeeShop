using System.Security.Cryptography;
using System.Text;

namespace CoffeeShopApp.Helpers;

public static class PasswordHasher
{
    private const int Iterations = 120_000;

    public static (string Hash, string Salt) Create(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256, 32);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public static bool Verify(string password, string salt, string expectedHash)
    {
        try
        {
            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), Convert.FromBase64String(salt), Iterations, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(actual, Convert.FromBase64String(expectedHash));
        }
        catch (FormatException) { return false; }
    }
}
