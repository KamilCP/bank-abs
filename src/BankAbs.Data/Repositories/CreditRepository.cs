using System.Data.Common;
using BankAbs.Data.Infrastructure;
using BankAbs.Domain.Common;
using BankAbs.Domain.Entities;
using static BankAbs.Data.Infrastructure.Database;

namespace BankAbs.Data.Repositories;

/// <summary>Кредитні договори і графіки погашення.</summary>
public sealed class CreditRepository(Database db)
{
    private static readonly Dictionary<string, string> SortColumns = new()
    {
        ["id"] = "k.id",
        ["client"] = "k.client_name",
        ["product"] = "k.product_name",
        ["amount"] = "k.amount",
        ["debt"] = "k.debt",
        ["issue"] = "k.issue_date",
        ["next"] = "k.next_due_date",
        ["status"] = "k.status",
    };

    /// <summary>Сторінка списку кредитів.</summary>
    public async Task<PagedResult<Credit>> ListAsync(ListQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var total = 0;
        var sql = """
            SELECT k.*, count(*) OVER () AS total_count FROM v_credit k
             WHERE (@search::text IS NULL OR k.client_name ILIKE @search OR k.account_number ILIKE @search
                    OR k.product_name ILIKE @search)
               AND (@status::text IS NULL OR k.status = @status)
               AND (@clientId::integer IS NULL OR k.client_id = @clientId)
               AND (@from::date IS NULL OR k.issue_date >= @from)
               AND (@to::date IS NULL OR k.issue_date <= @to)
            """ + SqlSort.OrderBy(SortColumns, query.SortBy, query.Descending, "issue", "k.id")
            + " LIMIT @limit OFFSET @offset";
        var items = await db.QueryAsync(sql, r =>
        {
            total = r.Get<int>("total_count");
            return Map(r);
        }, P("search", SqlSort.LikePattern(query.Search)), P("status", query.Status), P("clientId", query.OwnerId),
            P("from", query.DateFrom), P("to", query.DateTo), P("limit", query.PageSize), P("offset", query.Offset));
        return new PagedResult<Credit>(items, total, query.Page, query.PageSize);
    }

    /// <summary>Кредитний договір.</summary>
    public Task<Credit?> GetAsync(int id) =>
        db.QueryFirstAsync("SELECT * FROM v_credit WHERE id = @id", Map, P("id", id));

    /// <summary>Графік погашення кредиту.</summary>
    public Task<List<CreditScheduleItem>> ScheduleAsync(int creditId) =>
        db.QueryAsync("SELECT * FROM credit_schedule WHERE credit_id = @id ORDER BY payment_no",
            r => new CreditScheduleItem(r.Get<int>("payment_no"), r.Get<DateOnly>("due_date"), r.Get<decimal>("payment"),
                r.Get<decimal>("principal_part"), r.Get<decimal>("interest_part"), r.Get<decimal>("balance_after"),
                r.GetNullable<DateTime>("paid_at")),
            P("id", creditId));

    /// <summary>Видає кредит із зарахуванням на поточний рахунок (fn_issue_credit).</summary>
    /// <returns>Номер договору.</returns>
    public Task<int> IssueAsync(int clientId, int productId, decimal amount, int termMonths, int currentAccountId) =>
        db.ExecuteScalarAsync<int>("SELECT fn_issue_credit(@client, @product, @amount, @term, @account)",
            P("client", clientId), P("product", productId), P("amount", amount), P("term", termMonths),
            P("account", currentAccountId));

    /// <summary>Сплачує черговий платіж за графіком (fn_credit_payment).</summary>
    /// <returns>Номер операції.</returns>
    public Task<long> PayAsync(int creditId) =>
        db.ExecuteScalarAsync<long>("SELECT fn_credit_payment(@id)", P("id", creditId));

    internal static Credit Map(DbDataReader r) => new()
    {
        Id = r.Get<int>("id"),
        ClientId = r.Get<int>("client_id"),
        ClientName = r.Get<string>("client_name"),
        ProductName = r.Get<string>("product_name"),
        CurrencyCode = r.Get<string>("currency_code"),
        AccountNumber = r.Get<string>("account_number"),
        CurrentAccountId = r.Get<int>("current_account_id"),
        CurrentNumber = r.Get<string>("current_number"),
        Amount = r.Get<decimal>("amount"),
        Debt = r.Get<decimal>("debt"),
        AnnualRate = r.Get<decimal>("annual_rate"),
        TermMonths = r.Get<int>("term_months"),
        MonthlyPayment = r.Get<decimal>("monthly_payment"),
        IssueDate = r.Get<DateOnly>("issue_date"),
        Status = r.Get<string>("status"),
        NextDueDate = r.GetNullable<DateOnly>("next_due_date"),
        PaidCount = r.Get<int>("paid_count"),
    };
}
