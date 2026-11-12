namespace BankAbs.Domain.Banking;

/// <summary>Прогноз доходу за вкладом.</summary>
/// <param name="TotalInterest">Сума відсотків за весь строк.</param>
/// <param name="FinalAmount">Сума на рахунку вкладу в кінці строку.</param>
/// <param name="EffectiveRate">Ефективна річна ставка з урахуванням капіталізації, %.</param>
public sealed record DepositProjection(decimal TotalInterest, decimal FinalAmount, decimal EffectiveRate);

/// <summary>
/// Розрахунок відсотків за вкладом. Щомісячне нарахування збігається з серверною
/// функцією <c>fn_accrue_deposit_interest</c>.
/// </summary>
public static class DepositCalculator
{
    /// <summary>Відсотки за один місяць на суму <paramref name="baseAmount"/>.</summary>
    public static decimal MonthlyInterest(decimal baseAmount, decimal annualRatePercent) =>
        MoneyMath.RoundMoney(baseAmount * annualRatePercent / MoneyMath.Percent / MoneyMath.MonthsPerYear);

    /// <summary>Прогноз доходу на весь строк вкладу.</summary>
    public static DepositProjection Project(decimal principal, decimal annualRatePercent, int months, bool capitalization)
    {
        if (principal <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(principal), "Сума вкладу має бути більшою за нуль");
        }

        if (months <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(months), "Строк вкладу має бути більшим за нуль");
        }

        var balance = principal;
        var totalInterest = 0m;
        for (var month = 0; month < months; month++)
        {
            var interest = MonthlyInterest(capitalization ? balance : principal, annualRatePercent);
            totalInterest += interest;
            if (capitalization)
            {
                balance += interest;
            }
        }

        var years = (decimal)months / MoneyMath.MonthsPerYear;
        var effectiveRate = Math.Round(totalInterest / principal / years * MoneyMath.Percent, MoneyMath.MoneyScale,
            MidpointRounding.AwayFromZero);
        return new DepositProjection(totalInterest, balance, effectiveRate);
    }

    /// <summary>Відсотки при достроковому розірванні за ставкою «до запитання» (метод «факт/365»).</summary>
    public static decimal EarlyCloseInterest(decimal principal, decimal earlyRatePercent, int days) =>
        MoneyMath.RoundMoney(principal * earlyRatePercent / MoneyMath.Percent * days / MoneyMath.DaysPerYear);
}
