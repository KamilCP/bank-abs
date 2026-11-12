namespace BankAbs.Domain.Banking;

/// <summary>
/// Округлення грошових сум за правилом, що збігається з функцією round() PostgreSQL
/// для типу numeric (половина — від нуля).
/// </summary>
public static class MoneyMath
{
    /// <summary>Кількість знаків після коми для грошових сум.</summary>
    public const int MoneyScale = 2;

    /// <summary>Кількість знаків після коми для курсів валют.</summary>
    public const int RateScale = 6;

    /// <summary>Кількість місяців у році.</summary>
    public const int MonthsPerYear = 12;

    /// <summary>Кількість днів у році для розрахунку відсотків (метод «факт/365»).</summary>
    public const int DaysPerYear = 365;

    /// <summary>Множник для переведення відсотків у частку.</summary>
    public const decimal Percent = 100m;

    /// <summary>Округлює суму до копійок.</summary>
    public static decimal RoundMoney(decimal value) => Math.Round(value, MoneyScale, MidpointRounding.AwayFromZero);

    /// <summary>Округлює курс до 6 знаків.</summary>
    public static decimal RoundRate(decimal value) => Math.Round(value, RateScale, MidpointRounding.AwayFromZero);
}
