using System.Data.Common;
using BankAbs.Data.Infrastructure;
using BankAbs.Domain.Common;
using BankAbs.Domain.Entities;
using static BankAbs.Data.Infrastructure.Database;

namespace BankAbs.Data.Repositories;

/// <summary>Робота з таблицею клієнтів.</summary>
public sealed class ClientRepository(Database db)
{
    private static readonly Dictionary<string, string> SortColumns = new()
    {
        ["name"] = "c.full_name",
        ["taxId"] = "c.tax_id",
        ["type"] = "c.client_type",
        ["status"] = "c.status",
        ["created"] = "c.created_at",
        ["accounts"] = "open_accounts",
        ["balance"] = "total_balance_base",
    };

    private const string SelectSql = """
        SELECT c.*, count(*) OVER () AS total_count,
               (SELECT count(*) FROM account a WHERE a.client_id = c.id AND a.status = 'open')::integer AS open_accounts,
               coalesce((SELECT sum(v.balance_base) FROM v_account v
                          WHERE v.client_id = c.id AND v.account_type <> 'credit' AND v.status = 'open'), 0) AS total_balance_base
          FROM client c
        """;

    /// <summary>Сторінка списку клієнтів з пошуком, фільтрами і сортуванням.</summary>
    public async Task<PagedResult<Client>> ListAsync(ListQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var total = 0;
        var sql = SelectSql + """
             WHERE (@search::text IS NULL OR c.full_name ILIKE @search OR c.tax_id LIKE @search OR c.phone LIKE @search
                    OR c.email ILIKE @search)
               AND (@kind::text IS NULL OR c.client_type = @kind)
               AND (@status::text IS NULL OR c.status = @status)
            """ + SqlSort.OrderBy(SortColumns, query.SortBy, query.Descending, "name", "c.id")
            + " LIMIT @limit OFFSET @offset";
        var items = await db.QueryAsync(sql, r =>
        {
            total = r.Get<int>("total_count");
            return Map(r);
        }, P("search", SqlSort.LikePattern(query.Search)), P("kind", query.Kind), P("status", query.Status),
            P("limit", query.PageSize), P("offset", query.Offset));
        return new PagedResult<Client>(items, total, query.Page, query.PageSize);
    }

    /// <summary>Пошук клієнтів для вибору у формах (до 20 записів).</summary>
    public Task<List<Client>> SearchAsync(string? text) =>
        db.QueryAsync(SelectSql + """
             WHERE c.status = 'active' AND (@search::text IS NULL OR c.full_name ILIKE @search OR c.tax_id LIKE @search)
             ORDER BY c.full_name LIMIT 20
            """, Map, P("search", SqlSort.LikePattern(text)));

    /// <summary>Картка клієнта.</summary>
    public Task<Client?> GetAsync(int id) =>
        db.QueryFirstAsync(SelectSql + " WHERE c.id = @id", Map, P("id", id));

    /// <summary>Реєструє нового клієнта.</summary>
    /// <returns>Ідентифікатор клієнта.</returns>
    public Task<int> CreateAsync(Client client)
    {
        ArgumentNullException.ThrowIfNull(client);
        return db.ExecuteScalarAsync<int>("""
            INSERT INTO client (client_type, full_name, tax_id, document, birth_date, phone, email, address)
            VALUES (@type, @name, @taxId, @document, @birthDate, @phone, @email, @address)
            RETURNING id
            """, Params(client));
    }

    /// <summary>Зберігає зміни в картці клієнта.</summary>
    public Task<int> UpdateAsync(Client client)
    {
        ArgumentNullException.ThrowIfNull(client);
        return db.ExecuteAsync("""
            UPDATE client SET client_type = @type, full_name = @name, tax_id = @taxId, document = @document,
                   birth_date = @birthDate, phone = @phone, email = @email, address = @address
             WHERE id = @id
            """, [.. Params(client), P("id", client.Id)]);
    }

    /// <summary>Блокує або розблоковує клієнта.</summary>
    public Task<int> SetStatusAsync(int id, string status) =>
        db.ExecuteAsync("UPDATE client SET status = @status WHERE id = @id", P("status", status), P("id", id));

    private static SqlParam[] Params(Client c) =>
    [
        P("type", c.ClientType), P("name", c.FullName.Trim()), P("taxId", c.TaxId.Trim()),
        P("document", c.Document.Trim()), P("birthDate", c.ClientType == ClientTypes.Person ? c.BirthDate : null),
        P("phone", c.Phone.Trim()), P("email", c.Email.Trim()), P("address", c.Address.Trim()),
    ];

    private static Client Map(DbDataReader r) => new()
    {
        Id = r.Get<int>("id"),
        ClientType = r.Get<string>("client_type"),
        FullName = r.Get<string>("full_name"),
        TaxId = r.Get<string>("tax_id"),
        Document = r.Get<string>("document"),
        BirthDate = r.GetNullable<DateOnly>("birth_date"),
        Phone = r.Get<string>("phone"),
        Email = r.Get<string>("email"),
        Address = r.Get<string>("address"),
        Status = r.Get<string>("status"),
        CreatedAt = r.Get<DateTime>("created_at"),
        OpenAccounts = r.Get<int>("open_accounts"),
        TotalBalanceBase = r.Get<decimal>("total_balance_base"),
    };
}
