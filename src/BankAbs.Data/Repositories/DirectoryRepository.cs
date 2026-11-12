using System.Data.Common;
using BankAbs.Data.Infrastructure;
using BankAbs.Domain.Entities;
using static BankAbs.Data.Infrastructure.Database;

namespace BankAbs.Data.Repositories;

/// <summary>Довідники: валюти і курси, депозитні та кредитні продукти.</summary>
public sealed class DirectoryRepository(Database db)
{
    /// <summary>Усі валюти.</summary>
    public Task<List<Currency>> CurrenciesAsync() =>
        db.QueryAsync("SELECT * FROM currency ORDER BY is_base DESC, code", r => new Currency
        {
            Code = r.Get<string>("code").Trim(),
            Name = r.Get<string>("name"),
            Symbol = r.Get<string>("symbol"),
            IsBase = r.Get<bool>("is_base"),
            RateToBase = r.Get<decimal>("rate_to_base"),
            UpdatedAt = r.Get<DateTime>("updated_at"),
        });

    /// <summary>Змінює офіційний курс валюти.</summary>
    public Task<int> UpdateRateAsync(string code, decimal rate) =>
        db.ExecuteAsync("UPDATE currency SET rate_to_base = @rate WHERE code = @code AND NOT is_base",
            P("rate", rate), P("code", code));

    /// <summary>Депозитні продукти.</summary>
    public Task<List<DepositProduct>> DepositProductsAsync(bool activeOnly) =>
        db.QueryAsync("SELECT * FROM deposit_product WHERE is_active OR NOT @activeOnly ORDER BY currency_code <> 'UAH', currency_code, name",
            MapDepositProduct, P("activeOnly", activeOnly));

    /// <summary>Кредитні продукти.</summary>
    public Task<List<CreditProduct>> CreditProductsAsync(bool activeOnly) =>
        db.QueryAsync("SELECT * FROM credit_product WHERE is_active OR NOT @activeOnly ORDER BY currency_code <> 'UAH', currency_code, name",
            MapCreditProduct, P("activeOnly", activeOnly));

    /// <summary>Додає або змінює депозитний продукт.</summary>
    public Task<int> SaveDepositProductAsync(DepositProduct p)
    {
        ArgumentNullException.ThrowIfNull(p);
        SqlParam[] parameters =
        [
            P("id", p.Id), P("name", p.Name.Trim()), P("currency", p.CurrencyCode), P("rate", p.AnnualRate),
            P("term", p.TermMonths), P("cap", p.Capitalization), P("early", p.EarlyWithdrawal),
            P("min", p.MinAmount), P("active", p.IsActive),
        ];
        return p.Id == 0
            ? db.ExecuteScalarAsync<int>("""
                INSERT INTO deposit_product (name, currency_code, annual_rate, term_months, capitalization,
                                             early_withdrawal, min_amount, is_active)
                VALUES (@name, @currency, @rate, @term, @cap, @early, @min, @active) RETURNING id
                """, parameters)
            : db.ExecuteAsync("""
                UPDATE deposit_product SET name = @name, currency_code = @currency, annual_rate = @rate,
                       term_months = @term, capitalization = @cap, early_withdrawal = @early, min_amount = @min,
                       is_active = @active
                 WHERE id = @id
                """, parameters);
    }

    /// <summary>Додає або змінює кредитний продукт.</summary>
    public Task<int> SaveCreditProductAsync(CreditProduct p)
    {
        ArgumentNullException.ThrowIfNull(p);
        SqlParam[] parameters =
        [
            P("id", p.Id), P("name", p.Name.Trim()), P("currency", p.CurrencyCode), P("rate", p.AnnualRate),
            P("minTerm", p.MinTermMonths), P("maxTerm", p.MaxTermMonths), P("min", p.MinAmount),
            P("max", p.MaxAmount), P("active", p.IsActive),
        ];
        return p.Id == 0
            ? db.ExecuteScalarAsync<int>("""
                INSERT INTO credit_product (name, currency_code, annual_rate, min_term_months, max_term_months,
                                            min_amount, max_amount, is_active)
                VALUES (@name, @currency, @rate, @minTerm, @maxTerm, @min, @max, @active) RETURNING id
                """, parameters)
            : db.ExecuteAsync("""
                UPDATE credit_product SET name = @name, currency_code = @currency, annual_rate = @rate,
                       min_term_months = @minTerm, max_term_months = @maxTerm, min_amount = @min, max_amount = @max,
                       is_active = @active
                 WHERE id = @id
                """, parameters);
    }

    private static DepositProduct MapDepositProduct(DbDataReader r) => new()
    {
        Id = r.Get<int>("id"),
        Name = r.Get<string>("name"),
        CurrencyCode = r.Get<string>("currency_code").Trim(),
        AnnualRate = r.Get<decimal>("annual_rate"),
        TermMonths = r.Get<int>("term_months"),
        Capitalization = r.Get<bool>("capitalization"),
        EarlyWithdrawal = r.Get<bool>("early_withdrawal"),
        MinAmount = r.Get<decimal>("min_amount"),
        IsActive = r.Get<bool>("is_active"),
    };

    private static CreditProduct MapCreditProduct(DbDataReader r) => new()
    {
        Id = r.Get<int>("id"),
        Name = r.Get<string>("name"),
        CurrencyCode = r.Get<string>("currency_code").Trim(),
        AnnualRate = r.Get<decimal>("annual_rate"),
        MinTermMonths = r.Get<int>("min_term_months"),
        MaxTermMonths = r.Get<int>("max_term_months"),
        MinAmount = r.Get<decimal>("min_amount"),
        MaxAmount = r.Get<decimal>("max_amount"),
        IsActive = r.Get<bool>("is_active"),
    };
}
