namespace BankAbs.Domain.Entities;

/// <summary>Рахунок клієнта.</summary>
public sealed class Account
{
    /// <summary>Ідентифікатор.</summary>
    public int Id { get; init; }

    /// <summary>Номер рахунку IBAN.</summary>
    public string Number { get; init; } = string.Empty;

    /// <summary>Ідентифікатор клієнта.</summary>
    public int ClientId { get; init; }

    /// <summary>Назва клієнта.</summary>
    public string ClientName { get; init; } = string.Empty;

    /// <summary>Код валюти.</summary>
    public string CurrencyCode { get; init; } = string.Empty;

    /// <summary>Тип рахунку (<see cref="AccountTypes"/>).</summary>
    public string AccountType { get; init; } = AccountTypes.Current;

    /// <summary>Залишок (для кредитного — заборгованість).</summary>
    public decimal Balance { get; init; }

    /// <summary>Залишок у базовій валюті за офіційним курсом.</summary>
    public decimal BalanceBase { get; init; }

    /// <summary>Стан (<see cref="AccountStatuses"/>).</summary>
    public string Status { get; init; } = AccountStatuses.Open;

    /// <summary>Дата відкриття.</summary>
    public DateOnly OpenedAt { get; init; }

    /// <summary>Дата закриття.</summary>
    public DateOnly? ClosedAt { get; init; }

    /// <summary>Чи відкритий рахунок.</summary>
    public bool IsOpen => Status == AccountStatuses.Open;
}
