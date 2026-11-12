namespace BankAbs.Domain.Banking;

/// <summary>Рядок графіка погашення кредиту.</summary>
/// <param name="Number">Номер платежу.</param>
/// <param name="DueDate">Дата платежу.</param>
/// <param name="Payment">Сума платежу.</param>
/// <param name="Principal">Частина платежу в погашення тіла кредиту.</param>
/// <param name="Interest">Відсотки.</param>
/// <param name="BalanceAfter">Залишок заборгованості після платежу.</param>
public sealed record ScheduleRow(int Number, DateOnly DueDate, decimal Payment, decimal Principal, decimal Interest,
    decimal BalanceAfter);

/// <summary>
/// Розрахунок ануїтетного (рівними платежами) графіка погашення кредиту.
/// Алгоритм збігається з серверною функцією <c>fn_issue_credit</c>: щомісячні відсотки
/// нараховуються на залишок, останній платіж вирівнює залишок після округлень.
/// </summary>
public static class AnnuityCalculator
{
    /// <summary>Розмір щомісячного ануїтетного платежу.</summary>
    /// <param name="amount">Сума кредиту.</param>
    /// <param name="annualRatePercent">Річна ставка, %.</param>
    /// <param name="months">Строк у місяцях.</param>
    public static decimal MonthlyPayment(decimal amount, decimal annualRatePercent, int months)
    {
        ValidateArguments(amount, annualRatePercent, months);
        var monthlyRate = MonthlyRate(annualRatePercent);
        if (monthlyRate == 0)
        {
            return MoneyMath.RoundMoney(amount / months);
        }

        var growth = Power(1 + monthlyRate, months);
        return MoneyMath.RoundMoney(amount * monthlyRate * growth / (growth - 1));
    }

    /// <summary>Повний графік погашення.</summary>
    /// <param name="amount">Сума кредиту.</param>
    /// <param name="annualRatePercent">Річна ставка, %.</param>
    /// <param name="months">Строк у місяцях.</param>
    /// <param name="issueDate">Дата видачі; платежі — щомісяця в той самий день.</param>
    public static IReadOnlyList<ScheduleRow> BuildSchedule(decimal amount, decimal annualRatePercent, int months,
        DateOnly issueDate)
    {
        var payment = MonthlyPayment(amount, annualRatePercent, months);
        var monthlyRate = MonthlyRate(annualRatePercent);
        var rows = new List<ScheduleRow>(months);
        var balance = amount;
        for (var number = 1; number <= months; number++)
        {
            var interest = MoneyMath.RoundMoney(balance * monthlyRate);
            var principal = number == months ? balance : payment - interest;
            balance -= principal;
            rows.Add(new ScheduleRow(number, issueDate.AddMonths(number), principal + interest, principal, interest, balance));
        }

        return rows;
    }

    /// <summary>Загальна сума переплати (відсотків) за графіком.</summary>
    public static decimal TotalInterest(IEnumerable<ScheduleRow> schedule) => schedule.Sum(r => r.Interest);

    private static decimal MonthlyRate(decimal annualRatePercent) =>
        annualRatePercent / MoneyMath.Percent / MoneyMath.MonthsPerYear;

    private static decimal Power(decimal value, int exponent)
    {
        var result = 1m;
        for (var i = 0; i < exponent; i++)
        {
            result *= value;
        }

        return result;
    }

    private static void ValidateArguments(decimal amount, decimal annualRatePercent, int months)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Сума кредиту має бути більшою за нуль");
        }

        if (annualRatePercent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(annualRatePercent), "Ставка не може бути від'ємною");
        }

        if (months <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(months), "Строк кредиту має бути більшим за нуль");
        }
    }
}
