using System.Data.Common;
using BankAbs.Data.Infrastructure;
using BankAbs.Domain.Common;
using BankAbs.Domain.Entities;
using static BankAbs.Data.Infrastructure.Database;

namespace BankAbs.Data.Repositories;

/// <summary>Робота з рахунками.</summary>
public sealed class AccountRepository(Database db)
{
    private static readonly Dictionary<string, string> SortColumns = new()
    {
        ["number"] = "v.number",
        ["client"] = "v.client_name",
        ["type"] = "v.account_type",
        ["currency"] = "v.currency_code",
        ["balance"] = "v.balance_base",
        ["opened"] = "v.opened_at",
        ["status"] = "v.status",
    };

    /// <summary>Сторінка списку рахунків.</summary>
    public async Task<PagedResult<Account>> ListAsync(ListQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var total = 0;
        var sql = """
            SELECT v.*, count(*) OVER () AS total_count FROM v_account v
             WHERE (@search::text IS NULL OR v.number ILIKE @search OR v.client_name ILIKE @search)
               AND (@kind::text IS NULL OR v.account_type = @kind)
               AND (@currency::text IS NULL OR v.currency_code = @currency)
               AND (@status::text IS NULL OR v.status = @status)
               AND (@clientId::integer IS NULL OR v.client_id = @clientId)
            """ + SqlSort.OrderBy(SortColumns, query.SortBy, query.Descending, "opened", "v.id")
            + " LIMIT @limit OFFSET @offset";
        var items = await db.QueryAsync(sql, r =>
        {
            total = r.Get<int>("total_count");
            return Map(r);
        }, P("search", SqlSort.LikePattern(query.Search)), P("kind", query.Kind), P("currency", query.Currency),
            P("status", query.Status), P("clientId", query.OwnerId), P("limit", query.PageSize), P("offset", query.Offset));
        return new PagedResult<Account>(items, total, query.Page, query.PageSize);
    }

    /// <summary>Рахунки клієнта.</summary>
    public Task<List<Account>> ListByClientAsync(int clientId) =>
        db.QueryAsync("SELECT * FROM v_account WHERE client_id = @id ORDER BY status DESC, account_type, id", Map,
            P("id", clientId));

    /// <summary>Відкриті поточні рахунки для вибору у формах операцій.</summary>
    /// <param name="search">Частина номера рахунку або назви клієнта.</param>
    /// <param name="clientId">Обмеження клієнтом (необов'язково).</param>
    /// <param name="currency">Обмеження валютою (необов'язково).</param>
    /// <param name="limit">Максимальна кількість рахунків.</param>
    public Task<List<Account>> ListOpenCurrentAsync(string? search, int? clientId = null, string? currency = null, int limit = 500) =>
        db.QueryAsync("""
            SELECT * FROM v_account
             WHERE status = 'open' AND account_type = 'current'
               AND (@search::text IS NULL OR number ILIKE @search OR client_name ILIKE @search)
               AND (@clientId::integer IS NULL OR client_id = @clientId)
               AND (@currency::text IS NULL OR currency_code = @currency)
             ORDER BY client_name, currency_code, id LIMIT @limit
            """, Map, P("search", SqlSort.LikePattern(search)), P("clientId", clientId), P("currency", currency),
            P("limit", limit));

    /// <summary>Рахунок за номером IBAN (пробіли ігноруються).</summary>
    public Task<Account?> FindByNumberAsync(string number) =>
        db.QueryFirstAsync("SELECT * FROM v_account WHERE number = upper(replace(@number, ' ', ''))", Map, P("number", number));

    /// <summary>Рахунок за ідентифікатором.</summary>
    public Task<Account?> GetAsync(int id) =>
        db.QueryFirstAsync("SELECT * FROM v_account WHERE id = @id", Map, P("id", id));

    /// <summary>Відкриває поточний рахунок (серверна функція fn_open_account).</summary>
    /// <returns>Ідентифікатор нового рахунку.</returns>
    public Task<int> OpenAsync(int clientId, string currency) =>
        db.ExecuteScalarAsync<int>("SELECT fn_open_account(@clientId, @currency, 'current')",
            P("clientId", clientId), P("currency", currency));

    /// <summary>Закриває поточний рахунок з нульовим залишком (fn_close_account).</summary>
    public Task<int> CloseAsync(int id) =>
        db.ExecuteAsync("SELECT fn_close_account(@id)", P("id", id));

    internal static Account Map(DbDataReader r) => new()
    {
        Id = r.Get<int>("id"),
        Number = r.Get<string>("number"),
        ClientId = r.Get<int>("client_id"),
        ClientName = r.Get<string>("client_name"),
        CurrencyCode = r.Get<string>("currency_code"),
        AccountType = r.Get<string>("account_type"),
        Balance = r.Get<decimal>("balance"),
        BalanceBase = r.Get<decimal>("balance_base"),
        Status = r.Get<string>("status"),
        OpenedAt = r.Get<DateOnly>("opened_at"),
        ClosedAt = r.GetNullable<DateOnly>("closed_at"),
    };
}
