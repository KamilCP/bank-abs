using BankAbs.Data.Infrastructure;
using static BankAbs.Data.Infrastructure.Database;

namespace BankAbs.Data.Repositories;

/// <summary>Узагальнені і статистичні дані для головної сторінки та модуля аналізу.</summary>
public sealed class StatsRepository(Database db)
{
    /// <summary>Ключові показники на поточний момент.</summary>
    public async Task<KeyIndicators> IndicatorsAsync()
    {
        var result = await db.QueryFirstAsync("""
            SELECT (SELECT count(*) FROM client)::integer AS clients_total,
                   (SELECT count(*) FROM client WHERE created_at >= now() - interval '30 days')::integer AS clients_new,
                   (SELECT count(*) FROM account WHERE status = 'open')::integer AS open_accounts,
                   (SELECT coalesce(sum(balance_base), 0) FROM v_account WHERE account_type <> 'credit' AND status = 'open') AS funds,
                   (SELECT count(*) FROM deposit WHERE status = 'active')::integer AS deposits_active,
                   (SELECT coalesce(sum(balance_base), 0) FROM v_account WHERE account_type = 'deposit' AND status = 'open') AS deposits_base,
                   (SELECT count(*) FROM credit WHERE status <> 'repaid')::integer AS credits_active,
                   (SELECT coalesce(sum(balance_base), 0) FROM v_account WHERE account_type = 'credit' AND status = 'open') AS credits_base,
                   (SELECT count(*) FROM credit WHERE status = 'overdue')::integer AS overdue_count,
                   (SELECT coalesce(sum(v.balance_base), 0) FROM credit k JOIN v_account v ON v.id = k.account_id
                     WHERE k.status = 'overdue') AS overdue_base,
                   (SELECT count(*) FROM operation WHERE (op_time AT TIME ZONE 'Europe/Kyiv')::date = fn_today())::integer AS ops_today,
                   (SELECT coalesce(sum(amount_base), 0) FROM v_operation
                     WHERE (op_time AT TIME ZONE 'Europe/Kyiv')::date = fn_today()) AS turnover_today
            """, r => new KeyIndicators(r.Get<int>("clients_total"), r.Get<int>("clients_new"), r.Get<int>("open_accounts"),
            r.Get<decimal>("funds"), r.Get<int>("deposits_active"), r.Get<decimal>("deposits_base"),
            r.Get<int>("credits_active"), r.Get<decimal>("credits_base"), r.Get<int>("overdue_count"),
            r.Get<decimal>("overdue_base"), r.Get<int>("ops_today"), r.Get<decimal>("turnover_today")));
        return result!;
    }

    /// <summary>Кількість і оборот операцій по днях (дні без операцій — нулі).</summary>
    public Task<List<DailyPoint>> OperationsByDayAsync(DateOnly from, DateOnly to, string? opType) =>
        db.QueryAsync("""
            SELECT d::date AS day, count(o.id)::integer AS cnt, coalesce(sum(o.amount_base), 0) AS turnover
              FROM generate_series(@from::date, @to::date, interval '1 day') d
              LEFT JOIN v_operation o ON (o.op_time AT TIME ZONE 'Europe/Kyiv')::date = d::date
                                     AND (@type::text IS NULL OR o.op_type = @type)
             GROUP BY d ORDER BY d
            """, r => new DailyPoint(r.Get<DateOnly>("day"), r.Get<int>("cnt"), r.Get<decimal>("turnover")),
            P("from", from), P("to", to), P("type", opType));

    /// <summary>Оборот за типами операцій за період.</summary>
    public Task<List<CategoryValue>> TurnoverByTypeAsync(DateOnly from, DateOnly to) =>
        db.QueryAsync("""
            SELECT op_type, count(*)::integer AS cnt, sum(amount_base) AS total FROM v_operation
             WHERE (op_time AT TIME ZONE 'Europe/Kyiv')::date BETWEEN @from AND @to
             GROUP BY op_type ORDER BY total DESC
            """, r => new CategoryValue(r.Get<string>("op_type"), r.Get<int>("cnt"), r.Get<decimal>("total")),
            P("from", from), P("to", to));

    /// <summary>Кошти клієнтів за валютами.</summary>
    public Task<List<CurrencyFunds>> FundsByCurrencyAsync() =>
        db.QueryAsync("""
            SELECT currency_code,
                   coalesce(sum(balance) FILTER (WHERE account_type = 'current'), 0) AS cur,
                   coalesce(sum(balance) FILTER (WHERE account_type = 'deposit'), 0) AS dep,
                   coalesce(sum(balance_base), 0) AS total_base
              FROM v_account WHERE status = 'open' AND account_type <> 'credit'
             GROUP BY currency_code ORDER BY total_base DESC
            """, r => new CurrencyFunds(r.Get<string>("currency_code"), r.Get<decimal>("cur"), r.Get<decimal>("dep"),
            r.Get<decimal>("total_base")));

    /// <summary>Кредити за станом.</summary>
    public Task<List<CategoryValue>> CreditsByStatusAsync() =>
        db.QueryAsync("""
            SELECT k.status, count(*)::integer AS cnt, coalesce(sum(v.balance_base), 0) AS debt
              FROM credit k JOIN v_account v ON v.id = k.account_id GROUP BY k.status ORDER BY cnt DESC
            """, r => new CategoryValue(r.Get<string>("status"), r.Get<int>("cnt"), r.Get<decimal>("debt")));

    /// <summary>Клієнти з найбільшим обсягом коштів.</summary>
    public Task<List<TopClient>> TopClientsAsync(int count) =>
        db.QueryAsync("""
            SELECT c.id, c.full_name, c.client_type,
                   coalesce(sum(v.balance_base) FILTER (WHERE v.account_type <> 'credit' AND v.status = 'open'), 0) AS funds,
                   coalesce(sum(v.balance_base) FILTER (WHERE v.account_type = 'credit' AND v.status = 'open'), 0) AS debt
              FROM client c LEFT JOIN v_account v ON v.client_id = c.id
             GROUP BY c.id ORDER BY funds DESC LIMIT @count
            """, r => new TopClient(r.Get<int>("id"), r.Get<string>("full_name"), r.Get<string>("client_type"),
            r.Get<decimal>("funds"), r.Get<decimal>("debt")),
            P("count", count));
}
