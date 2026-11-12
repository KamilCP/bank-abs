namespace BankAbs.Domain.Security;

/// <summary>Правила складності пароля користувача.</summary>
public static class PasswordPolicy
{
    /// <summary>Мінімальна довжина пароля, якщо параметр системи не задано.</summary>
    public const int DefaultMinLength = 8;

    /// <summary>
    /// Перевіряє пароль: довжина не менше <paramref name="minLength"/>, є літера і цифра,
    /// пароль не збігається з логіном.
    /// </summary>
    /// <returns>Список порушень; порожній список означає, що пароль прийнятний.</returns>
    public static IReadOnlyList<string> Validate(string? password, string login, int minLength = DefaultMinLength)
    {
        var errors = new List<string>();
        if (string.IsNullOrEmpty(password))
        {
            errors.Add("Пароль не може бути порожнім");
            return errors;
        }

        if (password.Length < minLength)
        {
            errors.Add($"Пароль має містити щонайменше {minLength} символів");
        }

        if (!password.Any(char.IsLetter))
        {
            errors.Add("Пароль має містити хоча б одну літеру");
        }

        if (!password.Any(char.IsDigit))
        {
            errors.Add("Пароль має містити хоча б одну цифру");
        }

        if (string.Equals(password, login, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Пароль не повинен збігатися з логіном");
        }

        return errors;
    }
}
