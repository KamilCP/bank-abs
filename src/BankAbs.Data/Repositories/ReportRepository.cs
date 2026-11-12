using BankAbs.Data.Infrastructure;
using BankAbs.Domain.Common;
using BankAbs.Domain.Entities;
using static BankAbs.Data.Infrastructure.Database;

namespace BankAbs.Data.Repositories;

/// <summary>Дані звітів для друку.</summary>
public sealed class ReportRepository(Database db)
{
    /// <summary>Виписка по рахунку за період (fn_account_statement).</summary>
    public async Task<(StatementHeader Header, List<StatementLine> Lines)> StatementAsync(int accountId, DateOnly from, DateOnly to)
    {
        if (to < from)
        {
            throw new BusinessRuleException("Дата закінчення періоду не може бути раніше дати початку");
        }

        var lines = await db.QueryAsync("SELECT * FROM fn_account_statement(@id, @from, @to)",
            r => new StatementLine(r.Get<long>("op_id"), r.Get<DateTime>("op_time"), r.Get<string>("op_type"),
                r.Get<string>("description"), r.Get<string>("counterparty"), r.Get<decimal>("debit"),
                r.Get<decimal>("credit"), r.Get<decimal>("balance_after")),
            P("id", accountId), P("from", from), P("to", to));
        var header = await db.QueryFirstAsync("""
            SELECT a.number, c.full_name, c.tax_id, a.currency_code, a.account_type,
                   fn_account_balance_on(a.id, @from) AS opening,
                   fn_account_balance_on(a.id, (@to::date + 1)) AS closing
              FROM account a JOIN client c ON c.id = a.client_id WHERE a.id = @id
            """, r => new StatementHeader(r.Get<string>("number"), r.Get<string>("full_name"), r.Get<string>("tax_id"),
                r.Get<string>("currency_code"), r.Get<string>("account_type"), from, to, r.Get<decimal>("opening"),
                r.Get<decimal>("closing"), lines.Sum(l => l.Debit), lines.Sum(l => l.Credit)),
            P("id", accountId), P("from", from), P("to", to))
            ?? throw new BusinessRuleException("Рахунок не знайдено");
        return (header, lines);
    }

    /// <summary>Операції за день (для щоденного звіту).</summary>
    public Task<List<Operation>> DayOperationsAsync(DateOnly day) =>
        db.QueryAsync("""
            SELECT * FROM v_operation WHERE (op_time AT TIME ZONE 'Europe/Kyiv')::date = @day ORDER BY op_time, id
            """, OperationRepository.Map, P("day", day));

    /// <summary>Підсумки операцій за період за типом і валютою.</summary>
    public Task<List<OperationTotal>> TotalsByTypeAsync(DateOnly from, DateOnly to) =>
        db.QueryAsync("""
            SELECT op_type, currency_code, count(*)::integer AS cnt, sum(amount) AS amount, sum(amount_base) AS amount_base
              FROM v_operation
             WHERE (op_time AT TIME ZONE 'Europe/Kyiv')::date BETWEEN @from AND @to
             GROUP BY op_type, currency_code ORDER BY op_type, currency_code
            """, r => new OperationTotal(r.Get<string>("op_type"), r.Get<string>("currency_code"), r.Get<int>("cnt"),
                r.Get<decimal>("amount"), r.Get<decimal>("amount_base")),
            P("from", from), P("to", to));

    /// <summary>Підсумки операцій за період за виконавцями.</summary>
    public Task<List<UserTotal>> TotalsByUserAsync(DateOnly from, DateOnly to) =>
        db.QueryAsync("""
            SELECT coalesce(u.login, '—') AS login, coalesce(u.full_name, 'Система') AS full_name,
                   count(*)::integer AS cnt, sum(o.amount_base) AS amount_base
              FROM v_operation o LEFT JOIN app_user u ON u.id = o.user_id
             WHERE (o.op_time AT TIME ZONE 'Europe/Kyiv')::date BETWEEN @from AND @to
             GROUP BY u.login, u.full_name ORDER BY cnt DESC
            """, r => new UserTotal(r.Get<string>("login"), r.Get<string>("full_name"), r.Get<int>("cnt"),
                r.Get<decimal>("amount_base")),
            P("from", from), P("to", to));

    /// <summary>Портфель діючих депозитів за продуктами.</summary>
    public Task<List<PortfolioRow>> DepositPortfolioAsync() =>
        db.QueryAsync("""
            SELECT d.product_name, d.currency_code, count(*)::integer AS cnt, sum(d.principal) AS amount,
                   sum(d.current_amount) AS current, round(avg(d.annual_rate), 2) AS avg_rate, 0 AS overdue,
                   sum(round(d.current_amount * c.rate_to_base, 2)) AS amount_base
              FROM v_deposit d JOIN currency c ON c.code = d.currency_code
             WHERE d.status = 'active'
             GROUP BY d.product_name, d.currency_code ORDER BY amount_base DESC
            """, MapPortfolio);

    /// <summary>Портфель кредитів (діючі і прострочені) за продуктами.</summary>
    public Task<List<PortfolioRow>> CreditPortfolioAsync() =>
        db.QueryAsync("""
            SELECT k.product_name, k.currency_code, count(*)::integer AS cnt, sum(k.amount) AS amount,
                   sum(k.debt) AS current, round(avg(k.annual_rate), 2) AS avg_rate,
                   count(*) FILTER (WHERE k.status = 'overdue')::integer AS overdue,
                   sum(round(k.debt * c.rate_to_base, 2)) AS amount_base
              FROM v_credit k JOIN currency c ON c.code = k.currency_code
             WHERE k.status <> 'repaid'
             GROUP BY k.product_name, k.currency_code ORDER BY amount_base DESC
            """, MapPortfolio);

    /// <summary>Назва банку і відділення для заголовків звітів.</summary>
    public async Task<(string Bank, string Branch)> BankInfoAsync()
    {
        var rows = await db.QueryAsync(
            "SELECT key, value FROM system_parameter WHERE key IN ('bank.name', 'bank.branch')",
            r => (r.GetString(0), r.GetString(1)));
        var map = rows.ToDictionary(r => r.Item1, r => r.Item2);
        return (map.GetValueOrDefault("bank.name", "Bank ABS"), map.GetValueOrDefault("bank.branch", string.Empty));
    }

    private static PortfolioRow MapPortfolio(System.Data.Common.DbDataReader r) =>
        new(r.Get<string>("product_name"), r.Get<string>("currency_code"), r.Get<int>("cnt"), r.Get<decimal>("amount"),
            r.Get<decimal>("current"), r.Get<decimal>("avg_rate"), r.Get<int>("overdue"), r.Get<decimal>("amount_base"));
}
