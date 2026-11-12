using BankAbs.Domain.Banking;

namespace BankAbs.Tests.Domain;

public sealed class DepositCalculatorTests
{
    [Fact]
    public void MonthlyInterest_IsAnnualRateDividedBy12()
    {
        Assert.Equal(112.50m, DepositCalculator.MonthlyInterest(10000, 13.5m));
    }

    [Fact]
    public void Project_WithoutCapitalization_IsSimpleInterest()
    {
        var result = DepositCalculator.Project(10000, 13.5m, 12, capitalization: false);

        Assert.Equal(1350.00m, result.TotalInterest);
        Assert.Equal(10000m, result.FinalAmount);
        Assert.Equal(13.50m, result.EffectiveRate);
    }

    [Fact]
    public void Project_WithCapitalization_EarnsMoreThanSimpleInterest()
    {
        var simple = DepositCalculator.Project(10000, 14.5m, 12, capitalization: false);
        var compound = DepositCalculator.Project(10000, 14.5m, 12, capitalization: true);

        Assert.True(compound.TotalInterest > simple.TotalInterest);
        Assert.Equal(10000m + compound.TotalInterest, compound.FinalAmount);
        Assert.InRange(compound.EffectiveRate, 15.40m, 15.60m);
    }

    [Fact]
    public void EarlyCloseInterest_UsesActualDaysOf365()
    {
        Assert.Equal(164.38m, DepositCalculator.EarlyCloseInterest(10000, 1m, 600));
        Assert.Equal(0m, DepositCalculator.EarlyCloseInterest(10000, 1m, 0));
    }
}
