using BankAbs.Domain.Banking;

namespace BankAbs.Tests.Domain;

public sealed class CurrencyConverterTests
{
    [Fact]
    public void Rate_UahToUsd_AppliesMargin()
    {
        // 1 UAH = 1 / 41.70 USD, мінус 1,5 % маржі банку
        Assert.Equal(0.023621m, CurrencyConverter.Rate(1m, 41.70m, 1.5m));
    }

    [Fact]
    public void Rate_SameRates_WithoutMargin_IsOne()
    {
        Assert.Equal(1m, CurrencyConverter.Rate(48.40m, 48.40m, 0m));
    }

    [Fact]
    public void Convert_RoundsToKopecksHalfAwayFromZero()
    {
        Assert.Equal(98.50m, CurrencyConverter.Convert(4170m, 0.023621m));
        Assert.Equal(0.01m, CurrencyConverter.Convert(1m, 0.005m));
    }

    [Fact]
    public void Rate_NonPositiveCurrencyRate_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CurrencyConverter.Rate(0m, 1m, 0m));
    }
}
