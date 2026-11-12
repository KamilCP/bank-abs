using BankAbs.Domain.Banking;

namespace BankAbs.Tests.Domain;

public sealed class AnnuityCalculatorTests
{
    [Theory]
    [InlineData(100000, 12, 12, 8884.88)]
    [InlineData(30000, 29.9, 12, 2923.14)]
    [InlineData(12000, 0, 12, 1000.00)]
    public void MonthlyPayment_MatchesAnnuityFormula(decimal amount, decimal rate, int months, decimal expected)
    {
        Assert.Equal(expected, AnnuityCalculator.MonthlyPayment(amount, rate, months));
    }

    [Theory]
    [InlineData(100000, 12, 12)]
    [InlineData(26000, 29.9, 6)]
    [InlineData(65000, 34.9, 18)]
    [InlineData(1000, 0, 7)]
    public void BuildSchedule_RepaysWholeAmountAndEndsWithZeroBalance(decimal amount, decimal rate, int months)
    {
        var schedule = AnnuityCalculator.BuildSchedule(amount, rate, months, new DateOnly(2026, 1, 31));

        Assert.Equal(months, schedule.Count);
        Assert.Equal(amount, schedule.Sum(r => r.Principal));
        Assert.Equal(0m, schedule[^1].BalanceAfter);
        Assert.All(schedule, r => Assert.Equal(r.Payment, r.Principal + r.Interest));
    }

    [Fact]
    public void BuildSchedule_PaymentsAreEqualExceptLast_AndDatesAreMonthly()
    {
        var schedule = AnnuityCalculator.BuildSchedule(50000, 24, 6, new DateOnly(2026, 1, 31));
        var payment = AnnuityCalculator.MonthlyPayment(50000, 24, 6);

        Assert.All(schedule.Take(5), r => Assert.Equal(payment, r.Payment));
        Assert.InRange(schedule[^1].Payment, payment - 0.05m, payment + 0.05m);
        Assert.Equal(new DateOnly(2026, 2, 28), schedule[0].DueDate);
        Assert.Equal(new DateOnly(2026, 7, 31), schedule[^1].DueDate);
    }

    [Fact]
    public void InterestDecreasesAndPrincipalGrowsOverTime()
    {
        var schedule = AnnuityCalculator.BuildSchedule(100000, 18, 24, new DateOnly(2026, 3, 1));

        Assert.True(schedule[0].Interest > schedule[^2].Interest);
        Assert.True(schedule[0].Principal < schedule[^2].Principal);
    }

    [Theory]
    [InlineData(0, 10, 12)]
    [InlineData(1000, -1, 12)]
    [InlineData(1000, 10, 0)]
    public void MonthlyPayment_InvalidArguments_Throw(decimal amount, decimal rate, int months)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AnnuityCalculator.MonthlyPayment(amount, rate, months));
    }
}
