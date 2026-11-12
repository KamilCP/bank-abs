namespace BankAbs.Domain.Banking;

/// <summary>
/// Перевірка ідентифікаційних кодів: РНОКПП фізичної особи (10 цифр) і код ЄДРПОУ
/// юридичної особи (8 цифр) за контрольною цифрою.
/// </summary>
public static class TaxIdValidator
{
    /// <summary>Довжина РНОКПП.</summary>
    public const int PersonCodeLength = 10;

    /// <summary>Довжина коду ЄДРПОУ.</summary>
    public const int CompanyCodeLength = 8;

    private const int CheckModulus = 11;
    private const int SingleDigitModulus = 10;
    private const int BirthDatePartLength = 5;
    private static readonly DateOnly PersonCodeEpoch = new(1899, 12, 31);
    private static readonly int[] PersonWeights = [-1, 5, 7, 9, 4, 6, 10, 5, 7];
    private static readonly int[] CompanyWeightsLow = [1, 2, 3, 4, 5, 6, 7];
    private static readonly int[] CompanyWeightsMid = [7, 1, 2, 3, 4, 5, 6];
    private const int CompanyMidRangeStart = 30_000_000;
    private const int CompanyMidRangeEnd = 60_000_000;
    private const int CompanyRetryIncrement = 2;

    /// <summary>Перевіряє РНОКПП (реєстраційний номер облікової картки платника податків).</summary>
    public static bool IsValidPersonCode(string? code)
    {
        if (!IsDigits(code, PersonCodeLength))
        {
            return false;
        }

        var sum = 0;
        for (var i = 0; i < PersonWeights.Length; i++)
        {
            sum += PersonWeights[i] * Digit(code!, i);
        }

        var control = (((sum % CheckModulus) + CheckModulus) % CheckModulus) % SingleDigitModulus;
        return control == Digit(code!, PersonCodeLength - 1);
    }

    /// <summary>
    /// Дата народження, закодована в перших п'яти цифрах РНОКПП
    /// (кількість днів від 31.12.1899).
    /// </summary>
    public static DateOnly? GetBirthDate(string? personCode)
    {
        if (!IsDigits(personCode, PersonCodeLength))
        {
            return null;
        }

        var days = int.Parse(personCode![..BirthDatePartLength], System.Globalization.CultureInfo.InvariantCulture);
        return PersonCodeEpoch.AddDays(days);
    }

    /// <summary>Перевіряє код ЄДРПОУ юридичної особи.</summary>
    public static bool IsValidCompanyCode(string? code)
    {
        if (!IsDigits(code, CompanyCodeLength))
        {
            return false;
        }

        var number = int.Parse(code!, System.Globalization.CultureInfo.InvariantCulture);
        var weights = number is > CompanyMidRangeStart and < CompanyMidRangeEnd ? CompanyWeightsMid : CompanyWeightsLow;
        var control = WeightedRemainder(code!, weights, 0);
        if (control >= SingleDigitModulus)
        {
            control = WeightedRemainder(code!, weights, CompanyRetryIncrement);
            if (control >= SingleDigitModulus)
            {
                control = 0;
            }
        }

        return control == Digit(code!, CompanyCodeLength - 1);
    }

    private static int WeightedRemainder(string code, int[] weights, int increment)
    {
        var sum = 0;
        for (var i = 0; i < weights.Length; i++)
        {
            sum += (weights[i] + increment) * Digit(code, i);
        }

        return sum % CheckModulus;
    }

    private static bool IsDigits(string? value, int length) =>
        value is not null && value.Length == length && value.All(char.IsAsciiDigit);

    private static int Digit(string code, int index) => code[index] - '0';
}
