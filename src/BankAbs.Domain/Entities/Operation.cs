namespace BankAbs.Domain.Entities;

/// <summary>Фінансова операція (запис журналу операцій).</summary>
public sealed class Operation
{
    /// <summary>Номер операції.</summary>
    public long Id { get; init; }

    /// <summary>Час проведення (UTC).</summary>
    public DateTime OpTime { get; init; }

    /// <summary>Тип (<see cref="OperationTypes"/>).</summary>
    public string OpType { get; init; } = string.Empty;

    /// <summary>Сума у валюті списання.</summary>
    public decimal Amount { get; init; }

    /// <summary>Валюта операції.</summary>
    public string CurrencyCode { get; init; } = string.Empty;

    /// <summary>Сума зарахування після конвертації.</summary>
    public decimal? AmountTo { get; init; }

    /// <summary>Валюта зарахування (якщо відрізняється).</summary>
    public string? CurrencyTo { get; init; }

    /// <summary>Курс конвертації.</summary>
    public decimal? Rate { get; init; }

    /// <summary>Призначення.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Рахунок списання.</summary>
    public string? FromNumber { get; init; }

    /// <summary>Клієнт рахунку списання.</summary>
    public string? FromClient { get; init; }

    /// <summary>Рахунок зарахування.</summary>
    public string? ToNumber { get; init; }

    /// <summary>Клієнт рахунку зарахування.</summary>
    public string? ToClient { get; init; }

    /// <summary>Логін виконавця.</summary>
    public string UserLogin { get; init; } = string.Empty;

    /// <summary>Сума в базовій валюті.</summary>
    public decimal AmountBase { get; init; }
}

/// <summary>Рядок виписки по рахунку.</summary>
public sealed record StatementLine(long OperationId, DateTime OpTime, string OpType, string Description,
    string Counterparty, decimal Debit, decimal Credit, decimal BalanceAfter);
