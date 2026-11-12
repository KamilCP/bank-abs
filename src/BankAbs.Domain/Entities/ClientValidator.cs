using System.Text.RegularExpressions;
using BankAbs.Domain.Banking;

namespace BankAbs.Domain.Entities;

/// <summary>Перевірка картки клієнта перед збереженням.</summary>
public static partial class ClientValidator
{
    private const int MinNameLength = 3;
    private const int MaxNameLength = 200;
    private const int MinAgeYears = 14;

    /// <summary>Перевіряє дані клієнта.</summary>
    /// <param name="client">Клієнт.</param>
    /// <param name="today">Поточна дата (для перевірки віку).</param>
    /// <returns>Список помилок; порожній — дані коректні.</returns>
    public static IReadOnlyList<string> Validate(Client client, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(client);
        var errors = new List<string>();
        var name = client.FullName.Trim();
        if (name.Length < MinNameLength || name.Length > MaxNameLength)
        {
            errors.Add($"Найменування має містити від {MinNameLength} до {MaxNameLength} символів");
        }

        if (client.ClientType == ClientTypes.Person)
        {
            ValidatePerson(client, today, errors);
        }
        else if (client.ClientType == ClientTypes.Legal)
        {
            if (!TaxIdValidator.IsValidCompanyCode(client.TaxId))
            {
                errors.Add("Код ЄДРПОУ має складатися з 8 цифр і мати правильну контрольну цифру");
            }
        }
        else
        {
            errors.Add("Невідомий тип клієнта");
        }

        if (!string.IsNullOrWhiteSpace(client.Phone) && !PhoneRegex().IsMatch(client.Phone))
        {
            errors.Add("Телефон вводиться у форматі +380XXXXXXXXX");
        }

        if (!string.IsNullOrWhiteSpace(client.Email) && !EmailRegex().IsMatch(client.Email))
        {
            errors.Add("Невірний формат електронної пошти");
        }

        return errors;
    }

    private static void ValidatePerson(Client client, DateOnly today, List<string> errors)
    {
        if (!TaxIdValidator.IsValidPersonCode(client.TaxId))
        {
            errors.Add("РНОКПП має складатися з 10 цифр і мати правильну контрольну цифру");
            return;
        }

        if (client.BirthDate is null)
        {
            errors.Add("Для фізичної особи потрібно вказати дату народження");
            return;
        }

        if (TaxIdValidator.GetBirthDate(client.TaxId) != client.BirthDate)
        {
            errors.Add("Дата народження не відповідає РНОКПП");
        }

        if (client.BirthDate.Value.AddYears(MinAgeYears) > today)
        {
            errors.Add($"Клієнт має бути не молодшим за {MinAgeYears} років");
        }
    }

    [GeneratedRegex(@"^\+380\d{9}$")]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
