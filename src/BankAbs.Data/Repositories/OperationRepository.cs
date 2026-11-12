using System.Data.Common;
using BankAbs.Data.Infrastructure;
using BankAbs.Domain.Common;
using BankAbs.Domain.Entities;
using static BankAbs.Data.Infrastructure.Database;

namespace BankAbs.Data.Repositories;

/// <summary>Фінансові операції: журнал, касові операції, перекази.</summary>
public sealed class OperationRepository(Database db)
{
    private static readonly Dictionary<string, string> SortColumns = new()
    {
        ["time"] = "o.op_time",
        ["type"] = "o.op_type",
        ["amount"] = "o.amount_base",
        ["currency"] = "o.currency_code",
        ["user"] = "o.user_login",
    };

    /// <summary>Сторінка журналу операцій з фільтрами за періодом, типом, валютою і рахунком.</summary>
    public async Task<PagedResult<Operation>> ListAsync(ListQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var total = 0;
        var sql = """
            SELECT o.*, count(*) OVER () AS total_count FROM v_operation o
             WHERE (@from::date IS NULL OR (o.op_time AT TIME ZONE 'Europe/Kyiv')::date >= @from)
               AND (@to::date IS NULL OR (o.op_time AT TIME ZONE 'Europe/Kyiv')::date <= @to)
               AND (@kind::text IS NULL OR o.op_type = @kind)
               AND (@currency::text IS NULL OR o.currency_code = @currency)
               AND (@accountId::integer IS NULL OR o.from_account_id = @accountId OR o.to_account_id = @accountId)
               AND (@search::text IS NULL OR o.description ILIKE @search OR o.from_number ILIKE @search
                    OR o.to_number ILIKE @search OR o.from_client ILIKE @search OR o.to_client ILIKE @search
                    OR o.id::text = trim(both '%' from @search))
            """ + SqlSort.OrderBy(SortColumns, query.SortBy, query.Descending, "time", "o.id DESC")
            + " LIMIT @limit OFFSET @offset";
        var items = await db.QueryAsync(sql, r =>
        {
            total = r.Get<int>("total_count");
            return Map(r);
        }, P("from", query.DateFrom), P("to", query.DateTo), P("kind", query.Kind), P("currency", query.Currency),
            P("accountId", query.OwnerId), P("search", SqlSort.LikePattern(query.Search)),
            P("limit", query.PageSize), P("offset", query.Offset));
        return new PagedResult<Operation>(items, total, query.Page, query.PageSize);
    }

    /// <summary>Останні операції (для головної сторінки).</summary>
    public Task<List<Operation>> RecentAsync(int count) =>
        db.QueryAsync("SELECT * FROM v_operation ORDER BY op_time DESC, id DESC LIMIT @count", Map, P("count", count));

    /// <summary>Операція за номером.</summary>
    public Task<Operation?> GetAsync(long id) =>
        db.QueryFirstAsync("SELECT * FROM v_operation WHERE id = @id", Map, P("id", id));

    /// <summary>Внесення готівки на рахунок (fn_cash_in).</summary>
    /// <returns>Номер операції.</returns>
    public Task<long> CashInAsync(int accountId, decimal amount, string description) =>
        db.ExecuteScalarAsync<long>("SELECT fn_cash_in(@account, @amount, @description)",
            P("account", accountId), P("amount", amount), P("description", description));

    /// <summary>Видача готівки з рахунку (fn_cash_out).</summary>
    /// <returns>Номер операції.</returns>
    public Task<long> CashOutAsync(int accountId, decimal amount, string description) =>
        db.ExecuteScalarAsync<long>("SELECT fn_cash_out(@account, @amount, @description)",
            P("account", accountId), P("amount", amount), P("description", description));

    /// <summary>Переказ між рахунками з конвертацією (fn_transfer).</summary>
    /// <returns>Номер операції.</returns>
    public Task<long> TransferAsync(int fromId, int toId, decimal amount, string description) =>
        db.ExecuteScalarAsync<long>("SELECT fn_transfer(@from, @to, @amount, @description)",
            P("from", fromId), P("to", toId), P("amount", amount), P("description", description));

    /// <summary>Курс конвертації банку між валютами (fn_exchange_rate).</summary>
    public Task<decimal> ExchangeRateAsync(string from, string to) =>
        db.ScalarAsync<decimal>("SELECT fn_exchange_rate(@from, @to)", P("from", from), P("to", to));

    internal static Operation Map(DbDataReader r) => new()
    {
        Id = r.Get<long>("id"),
        OpTime = r.Get<DateTime>("op_time"),
        OpType = r.Get<string>("op_type"),
        Amount = r.Get<decimal>("amount"),
        CurrencyCode = r.Get<string>("currency_code"),
        AmountTo = r.GetNullable<decimal>("amount_to"),
        CurrencyTo = r.GetNullableString("currency_to"),
        Rate = r.GetNullable<decimal>("rate"),
        Description = r.Get<string>("description"),
        FromNumber = r.GetNullableString("from_number"),
        FromClient = r.GetNullableString("from_client"),
        ToNumber = r.GetNullableString("to_number"),
        ToClient = r.GetNullableString("to_client"),
        UserLogin = r.Get<string>("user_login"),
        AmountBase = r.Get<decimal>("amount_base"),
    };
}
