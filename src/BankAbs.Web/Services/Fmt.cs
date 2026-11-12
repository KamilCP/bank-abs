using System.Globalization;

namespace BankAbs.Web.Services;

/// <summary>Форматування значень для відображення відповідно до стандарту інтерфейсу.</summary>
public static class Fmt
{
    /// <summary>Культура інтерфейсу.</summary>
    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("uk-UA");

    private static readonly TimeZoneInfo LocalZone = FindZone();

    /// <summary>Сума з двома знаками після коми і розділенням розрядів: «12 345,67».</summary>
    public static string Money(decimal value) => value.ToString("N2", Culture);

    /// <summary>Сума з кодом валюти: «12 345,67 UAH».</summary>
    public static string Money(decimal value, string? currency) => Money(value) + " " + currency?.Trim();

    /// <summary>Сума в базовій валюті без копійок: «12 346 ₴».</summary>
    public static string MoneyShort(decimal value) => value.ToString("N0", Culture) + " ₴";

    /// <summary>Скорочений запис великих сум для діаграм: «1,2 млн».</summary>
    public static string Compact(decimal value) => Math.Abs(value) switch
    {
        >= 1_000_000m => (value / 1_000_000m).ToString("0.#", Culture) + " млн",
        >= 1_000m => (value / 1_000m).ToString("0.#", Culture) + " тис.",
        _ => value.ToString("0", Culture),
    };

    /// <summary>Ціле число з розділенням розрядів.</summary>
    public static string Number(int value) => value.ToString("N0", Culture);

    /// <summary>Відсоткова ставка: «13,50 %».</summary>
    public static string Rate(decimal value) => value.ToString("N2", Culture) + " %";

    /// <summary>Курс валюти.</summary>
    public static string ExchangeRate(decimal value) => value.ToString("0.0000##", Culture);

    /// <summary>Дата: «12.11.2026».</summary>
    public static string Date(DateOnly? value) => value?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? "—";

    /// <summary>Дата і час у часовому поясі банку: «12.11.2026 14:05».</summary>
    public static string DateTime(System.DateTime? utc) =>
        utc is null ? "—" : ToLocal(utc.Value).ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);

    /// <summary>Час: «14:05:33».</summary>
    public static string Time(System.DateTime utc) => ToLocal(utc).ToString("HH:mm:ss", CultureInfo.InvariantCulture);

    /// <summary>Номер рахунку групами по 4 символи.</summary>
    public static string Iban(string? number) => BankAbs.Domain.Banking.Iban.Format(number);

    /// <summary>Поточна дата в часовому поясі банку.</summary>
    public static DateOnly Today => DateOnly.FromDateTime(ToLocal(System.DateTime.UtcNow));

    /// <summary>Переводить час UTC у місцевий час банку (Europe/Kyiv).</summary>
    public static System.DateTime ToLocal(System.DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(System.DateTime.SpecifyKind(utc, DateTimeKind.Utc), LocalZone);

    private static TimeZoneInfo FindZone()
    {
        foreach (var id in new[] { "Europe/Kyiv", "Europe/Kiev", "FLE Standard Time" })
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone))
            {
                return zone;
            }
        }

        return TimeZoneInfo.Utc;
    }
}
