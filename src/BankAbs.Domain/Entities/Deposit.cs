namespace BankAbs.Domain.Entities;

/// <summary>Договір депозиту (вкладу).</summary>
public sealed class Deposit
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

    /// <summary>Рахунок вкладу.</summary>
    public int AccountId { get; init; }

    /// <summary>Номер рахунку вкладу.</summary>
    public string AccountNumber { get; init; } = string.Empty;

    /// <summary>Номер рахунку виплати.</summary>
    public string PayoutNumber { get; init; } = string.Empty;

    /// <summary>Сума вкладу.</summary>
    public decimal Principal { get; init; }

    /// <summary>Поточна сума на рахунку вкладу.</summary>
    public decimal CurrentAmount { get; init; }

    /// <summary>Річна ставка, %.</summary>
    public decimal AnnualRate { get; init; }

    /// <summary>Капіталізація.</summary>
    public bool Capitalization { get; init; }

    /// <summary>Дата розміщення.</summary>
    public DateOnly StartDate { get; init; }

    /// <summary>Дата завершення строку.</summary>
    public DateOnly EndDate { get; init; }

    /// <summary>Дата останнього нарахування.</summary>
    public DateOnly LastAccrualDate { get; init; }

    /// <summary>Нараховано відсотків.</summary>
    public decimal InterestPaid { get; init; }

    /// <summary>Стан (<see cref="DepositStatuses"/>).</summary>
    public string Status { get; init; } = DepositStatuses.Active;

    /// <summary>Дата закриття.</summary>
    public DateOnly? ClosedAt { get; init; }

    /// <summary>Дозволено дострокове розірвання.</summary>
    public bool EarlyWithdrawal { get; init; }
}
