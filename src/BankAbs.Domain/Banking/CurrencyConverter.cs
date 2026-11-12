namespace BankAbs.Domain.Banking;

/// <summary>
/// Розрахунок курсу конвертації між валютами через базову валюту з урахуванням маржі банку.
/// Дублює логіку серверної функції <c>fn_exchange_rate</c> для попереднього розрахунку у формі.
/// </summary>
public static class CurrencyConverter
{
    /// <summary>Курс конвертації: скільки одиниць валюти зарахування дають за 1 одиницю валюти списання.</summary>
    /// <param name="fromRateToBase">Курс валюти списання до базової.</param>
    /// <param name="toRateToBase">Курс валюти зарахування до базової.</param>
    /// <param name="marginPercent">Маржа банку у відсотках.</param>
    public static decimal Rate(decimal fromRateToBase, decimal toRateToBase, decimal marginPercent)
    {
        if (fromRateToBase <= 0 || toRateToBase <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fromRateToBase), "Курс валюти має бути більшим за нуль");
        }

        return MoneyMath.RoundRate(fromRateToBase / toRateToBase * (1 - (marginPercent / MoneyMath.Percent)));
    }

    /// <summary>Сума після конвертації, округлена до копійок.</summary>
    public static decimal Convert(decimal amount, decimal rate) => MoneyMath.RoundMoney(amount * rate);
}
