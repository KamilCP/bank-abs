using System.Data.Common;
using BankAbs.Data.Infrastructure;
using BankAbs.Domain.Common;
using BankAbs.Domain.Entities;
using static BankAbs.Data.Infrastructure.Database;

namespace BankAbs.Data.Repositories;

/// <summary>Депозитні договори.</summary>
public sealed class DepositRepository(Database db)
{
    private static readonly Dictionary<string, string> SortColumns = new()
    {
        ["id"] = "d.id",
        ["client"] = "d.client_name",
        ["product"] = "d.product_name",
        ["amount"] = "d.current_amount",
        ["rate"] = "d.annual_rate",
        ["start"] = "d.start_date",
        ["end"] = "d.end_date",
        ["status"] = "d.status",
    };

    /// <summary>Сторінка списку депозитів.</summary>
    public async Task<PagedResult<Deposit>> ListAsync(ListQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var total = 0;
        var sql = """
            SELECT d.*, count(*) OVER () AS total_count FROM v_deposit d
             WHERE (@search::text IS NULL OR d.client_name ILIKE @search OR d.account_number ILIKE @search
                    OR d.product_name ILIKE @search)
               AND (@status::text IS NULL OR d.status = @status)
               AND (@currency::text IS NULL OR d.currency_code = @currency)
               AND (@clientId::integer IS NULL OR d.client_id = @clientId)
               AND (@from::date IS NULL OR d.start_date >= @from)
               AND (@to::date IS NULL OR d.start_date <= @to)
            """ + SqlSort.OrderBy(SortColumns, query.SortBy, query.Descending, "start", "d.id")
            + " LIMIT @limit OFFSET @offset";
        var items = await db.QueryAsync(sql, r =>
        {
            total = r.Get<int>("total_count");
            return Map(r);
        }, P("search", SqlSort.LikePattern(query.Search)), P("status", query.Status), P("currency", query.Currency),
            P("clientId", query.OwnerId), P("from", query.DateFrom), P("to", query.DateTo),
            P("limit", query.PageSize), P("offset", query.Offset));
        return new PagedResult<Deposit>(items, total, query.Page, query.PageSize);
    }

    /// <summary>Договір депозиту.</summary>
    public Task<Deposit?> GetAsync(int id) =>
        db.QueryFirstAsync("SELECT * FROM v_deposit WHERE id = @id", Map, P("id", id));

    /// <summary>Відкриває вклад зі списанням коштів з поточного рахунку (fn_open_deposit).</summary>
    /// <returns>Номер договору.</returns>
    public Task<int> OpenAsync(int clientId, int productId, decimal amount, int sourceAccountId) =>
        db.ExecuteScalarAsync<int>("SELECT fn_open_deposit(@client, @product, @amount, @source)",
            P("client", clientId), P("product", productId), P("amount", amount), P("source", sourceAccountId));

    /// <summary>Достроково розриває вклад (fn_close_deposit_early).</summary>
    /// <returns>Сума, виплачена клієнту.</returns>
    public Task<decimal> CloseEarlyAsync(int id) =>
        db.ExecuteScalarAsync<decimal>("SELECT fn_close_deposit_early(@id)", P("id", id));

    internal static Deposit Map(DbDataReader r) => new()
    {
        Id = r.Get<int>("id"),
        ClientId = r.Get<int>("client_id"),
        ClientName = r.Get<string>("client_name"),
        ProductName = r.Get<string>("product_name"),
        CurrencyCode = r.Get<string>("currency_code"),
        AccountId = r.Get<int>("account_id"),
        AccountNumber = r.Get<string>("account_number"),
        PayoutNumber = r.Get<string>("payout_number"),
        Principal = r.Get<decimal>("principal"),
        CurrentAmount = r.Get<decimal>("current_amount"),
        AnnualRate = r.Get<decimal>("annual_rate"),
        Capitalization = r.Get<bool>("capitalization"),
        StartDate = r.Get<DateOnly>("start_date"),
        EndDate = r.Get<DateOnly>("end_date"),
        LastAccrualDate = r.Get<DateOnly>("last_accrual_date"),
        InterestPaid = r.Get<decimal>("interest_paid"),
        Status = r.Get<string>("status"),
        ClosedAt = r.GetNullable<DateOnly>("closed_at"),
        EarlyWithdrawal = r.Get<bool>("early_withdrawal"),
    };
}
