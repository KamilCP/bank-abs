using System.Globalization;
using System.Security.Cryptography;

namespace BankAbs.Domain.Security;

/// <summary>
/// Хешування паролів за алгоритмом PBKDF2-HMAC-SHA256 з випадковою сіллю.
/// Формат збереженого значення: <c>PBKDF2-SHA256$ітерації$сіль(Base64)$хеш(Base64)</c>.
/// </summary>
public static class PasswordHasher
{
    private const string AlgorithmTag = "PBKDF2-SHA256";
    private const int DefaultIterations = 100_000;
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    private const int HashPartCount = 4;

    /// <summary>Обчислює хеш пароля з новою випадковою сіллю.</summary>
    /// <param name="password">Пароль у відкритому вигляді.</param>
    /// <returns>Рядок для збереження в БД.</returns>
    public static string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        return Hash(password, salt, DefaultIterations);
    }

    /// <summary>Обчислює хеш пароля з заданою сіллю (використовується тестами і початковими даними).</summary>
    public static string Hash(string password, byte[] salt, int iterations)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(salt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, HashSizeBytes);
        return string.Join('$', AlgorithmTag, iterations.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    /// <summary>Перевіряє пароль за збереженим хешем (порівняння за сталий час).</summary>
    /// <returns><c>true</c>, якщо пароль правильний.</returns>
    public static bool Verify(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
        {
            return false;
        }

        var parts = storedHash.Split('$');
        if (parts.Length != HashPartCount || parts[0] != AlgorithmTag
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations))
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
