namespace BankAbs.Domain.Entities;

/// <summary>Кредитний договір.</summary>
public sealed class Credit
{
    /// <summary>Номер договору.</summary>
    public int Id { get; init; }

    /// <summary>Клієнт.</summary>
    public int ClientId { get; init; }

    /// <summary>Назва клієнта.</summary>
    public string ClientName { get; init; } = string.Empty;

    /// <summary>Продукт.</summary>
    public string ProductName { get; init; } = string.Empty;

    /// <summary>Валюта.</summary>
    public string CurrencyCode { get; init; } = string.Empty;

    /// <summary>Номер кредитного рахунку.</summary>
    public string AccountNumber { get; init; } = string.Empty;

    /// <summary>Поточний рахунок для зарахування і списання платежів.</summary>
    public int CurrentAccountId { get; init; }

    /// <summary>Номер поточного рахунку.</summary>
    public string CurrentNumber { get; init; } = string.Empty;

    /// <summary>Сума кредиту.</summary>
    public decimal Amount { get; init; }

    /// <summary>Залишок заборгованості.</summary>
    public decimal Debt { get; init; }

    /// <summary>Річна ставка, %.</summary>
    public decimal AnnualRate { get; init; }

    /// <summary>Строк, міс.</summary>
    public int TermMonths { get; init; }

    /// <summary>Щомісячний платіж.</summary>
    public decimal MonthlyPayment { get; init; }

    /// <summary>Дата видачі.</summary>
    public DateOnly IssueDate { get; init; }

    /// <summary>Стан (<see cref="CreditStatuses"/>).</summary>
    public string Status { get; init; } = CreditStatuses.Active;

    /// <summary>Дата наступного платежу.</summary>
    public DateOnly? NextDueDate { get; init; }

    /// <summary>Кількість сплачених платежів.</summary>
    public int PaidCount { get; init; }
}

/// <summary>Рядок графіка погашення з відміткою про сплату.</summary>
public sealed record CreditScheduleItem(int Number, DateOnly DueDate, decimal Payment, decimal Principal,
    decimal Interest, decimal BalanceAfter, DateTime? PaidAt);
