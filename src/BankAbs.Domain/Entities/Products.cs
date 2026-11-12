namespace BankAbs.Domain.Entities;

/// <summary>Валюта і її офіційний курс.</summary>
public sealed class Currency
{
    /// <summary>Код ISO 4217.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Назва.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Символ.</summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>Чи є базовою (гривня).</summary>
    public bool IsBase { get; set; }

    /// <summary>Курс до базової валюти.</summary>
    public decimal RateToBase { get; set; }

    /// <summary>Час останньої зміни курсу.</summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Депозитний продукт (умови вкладу).</summary>
public sealed class DepositProduct
{
    /// <summary>Ідентифікатор.</summary>
    public int Id { get; set; }

    /// <summary>Назва.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Валюта.</summary>
    public string CurrencyCode { get; set; } = "UAH";

    /// <summary>Річна ставка, %.</summary>
    public decimal AnnualRate { get; set; }

    /// <summary>Строк, міс.</summary>
    public int TermMonths { get; set; } = 12;

    /// <summary>Капіталізація відсотків.</summary>
    public bool Capitalization { get; set; }

    /// <summary>Дозволено дострокове розірвання.</summary>
    public bool EarlyWithdrawal { get; set; } = true;

    /// <summary>Мінімальна сума.</summary>
    public decimal MinAmount { get; set; }

    /// <summary>Доступний для нових договорів.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Копія для редагування.</summary>
    public DepositProduct Clone() => (DepositProduct)MemberwiseClone();
}

/// <summary>Кредитний продукт.</summary>
public sealed class CreditProduct
{
    /// <summary>Ідентифікатор.</summary>
    public int Id { get; set; }

    /// <summary>Назва.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Валюта.</summary>
    public string CurrencyCode { get; set; } = "UAH";

    /// <summary>Річна ставка, %.</summary>
    public decimal AnnualRate { get; set; }

    /// <summary>Мінімальний строк, міс.</summary>
    public int MinTermMonths { get; set; } = 1;

    /// <summary>Максимальний строк, міс.</summary>
    public int MaxTermMonths { get; set; } = 12;

    /// <summary>Мінімальна сума.</summary>
    public decimal MinAmount { get; set; }

    /// <summary>Максимальна сума.</summary>
    public decimal MaxAmount { get; set; }

    /// <summary>Доступний для нових договорів.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Копія для редагування.</summary>
    public CreditProduct Clone() => (CreditProduct)MemberwiseClone();
}
