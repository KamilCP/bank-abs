using System.Text;

namespace BankAbs.Domain.Banking;

/// <summary>
/// Номер рахунку у форматі IBAN для України: <c>UA</c> + 2 контрольні цифри + 6 цифр коду банку
/// + 19 цифр номера рахунку (усього 29 символів). Контрольні цифри — MOD 97-10 (ISO 13616).
/// </summary>
public static class Iban
{
    /// <summary>Довжина українського IBAN.</summary>
    public const int UkraineLength = 29;

    private const string CountryCode = "UA";
    private const int BankCodeLength = 6;
    private const int AccountLength = 19;
    private const int Modulus = 97;
    private const int ValidRemainder = 1;
    private const int CheckBase = 98;
    private const int LetterOffset = 'A' - 10;
    private const int GroupSize = 4;

    /// <summary>Перевіряє формат і контрольні цифри українського IBAN.</summary>
    public static bool IsValid(string? iban)
    {
        if (string.IsNullOrWhiteSpace(iban))
        {
            return false;
        }

        var value = Normalize(iban);
        if (value.Length != UkraineLength || !value.StartsWith(CountryCode, StringComparison.Ordinal)
            || !value[2..].All(char.IsAsciiDigit))
        {
            return false;
        }

        return Mod97(value[4..] + value[..4]) == ValidRemainder;
    }

    /// <summary>Створює IBAN з коду банку і номера рахунку, обчислюючи контрольні цифри.</summary>
    /// <param name="bankCode">Код банку (6 цифр).</param>
    /// <param name="account">Номер рахунку (до 19 цифр, доповнюється нулями зліва).</param>
    public static string Create(string bankCode, string account)
    {
        ArgumentNullException.ThrowIfNull(bankCode);
        ArgumentNullException.ThrowIfNull(account);
        if (bankCode.Length != BankCodeLength || !bankCode.All(char.IsAsciiDigit))
        {
            throw new ArgumentException("Код банку має складатися з 6 цифр", nameof(bankCode));
        }

        if (account.Length > AccountLength || !account.All(char.IsAsciiDigit))
        {
            throw new ArgumentException("Номер рахунку має містити не більше 19 цифр", nameof(account));
        }

        var bban = bankCode + account.PadLeft(AccountLength, '0');
        var check = CheckBase - Mod97(bban + CountryCode + "00");
        return CountryCode + check.ToString("00", System.Globalization.CultureInfo.InvariantCulture) + bban;
    }

    /// <summary>Форматує IBAN групами по 4 символи для відображення.</summary>
    public static string Format(string? iban)
    {
        if (string.IsNullOrEmpty(iban))
        {
            return string.Empty;
        }

        var value = Normalize(iban);
        var sb = new StringBuilder(value.Length + (value.Length / GroupSize));
        for (var i = 0; i < value.Length; i++)
        {
            if (i > 0 && i % GroupSize == 0)
            {
                sb.Append(' ');
            }

            sb.Append(value[i]);
        }

        return sb.ToString();
    }

    /// <summary>Прибирає пробіли і переводить у верхній регістр.</summary>
    public static string Normalize(string iban) =>
        new(iban.Where(c => !char.IsWhiteSpace(c)).Select(char.ToUpperInvariant).ToArray());

    /// <summary>Остача від ділення на 97 числа, записаного рядком (літери замінюються числами A=10..Z=35).</summary>
    private static int Mod97(string value)
    {
        var remainder = 0;
        foreach (var c in value)
        {
            var digits = char.IsAsciiDigit(c) ? (c - '0').ToString(System.Globalization.CultureInfo.InvariantCulture)
                                              : (c - LetterOffset).ToString(System.Globalization.CultureInfo.InvariantCulture);
            foreach (var d in digits)
            {
                remainder = ((remainder * 10) + (d - '0')) % Modulus;
            }
        }

        return remainder;
    }
}
