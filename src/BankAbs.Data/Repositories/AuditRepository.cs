using BankAbs.Data.Infrastructure;
using BankAbs.Domain.Common;
using BankAbs.Domain.Entities;
using static BankAbs.Data.Infrastructure.Database;

namespace BankAbs.Data.Repositories;

/// <summary>Журнал системи (таблиця audit_log).</summary>
public sealed class AuditRepository(Database db)
{
    private static readonly Dictionary<string, string> SortColumns = new()
    {
        ["time"] = "a.event_time",
        ["user"] = "a.user_login",
        ["type"] = "a.event_type",
        ["table"] = "a.table_name",
    };

    /// <summary>Сторінка журналу з фільтрами за періодом, користувачем, типом події і таблицею.</summary>
    /// <param name="query">Параметри: OwnerId — користувач, Kind — тип події, Status — таблиця.</param>
    public async Task<PagedResult<AuditEntry>> ListAsync(ListQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var total = 0;
        var sql = """
            SELECT a.*, count(*) OVER () AS total_count FROM audit_log a
             WHERE (@from::date IS NULL OR (a.event_time AT TIME ZONE 'Europe/Kyiv')::date >= @from)
               AND (@to::date IS NULL OR (a.event_time AT TIME ZONE 'Europe/Kyiv')::date <= @to)
               AND (@userId::integer IS NULL OR a.user_id = @userId)
               AND (@kind::text IS NULL OR a.event_type = @kind)
               AND (@table::text IS NULL OR a.table_name = @table)
               AND (@search::text IS NULL OR a.summary ILIKE @search OR a.record_id = trim(both '%' from @search)
                    OR a.details::text ILIKE @search)
            """ + SqlSort.OrderBy(SortColumns, query.SortBy, query.Descending, "time", "a.id DESC")
            + " LIMIT @limit OFFSET @offset";
        var items = await db.QueryAsync(sql, r =>
        {
            total = r.Get<int>("total_count");
            return new AuditEntry
            {
                Id = r.Get<long>("id"),
                EventTime = r.Get<DateTime>("event_time"),
                UserLogin = r.Get<string>("user_login"),
                EventType = r.Get<string>("event_type"),
                TableName = r.Get<string>("table_name"),
                RecordId = r.Get<string>("record_id"),
                Summary = r.Get<string>("summary"),
                Details = r.GetNullableString("details"),
                IpAddress = r.Get<string>("ip_address"),
            };
        }, P("from", query.DateFrom), P("to", query.DateTo), P("userId", query.OwnerId), P("kind", query.Kind),
            P("table", query.Status), P("search", SqlSort.LikePattern(query.Search)),
            P("limit", query.PageSize), P("offset", query.Offset));
        return new PagedResult<AuditEntry>(items, total, query.Page, query.PageSize);
    }

    /// <summary>Таблиці, що зустрічаються в журналі (для фільтра).</summary>
    public Task<List<string>> TablesAsync() =>
        db.QueryAsync("SELECT DISTINCT table_name FROM audit_log WHERE table_name <> '' ORDER BY 1", r => r.GetString(0));

    /// <summary>Записує подію в журнал від імені поточного користувача (fn_log_event).</summary>
    /// <param name="eventType">Тип події (<see cref="AuditEventTypes"/>).</param>
    /// <param name="table">Таблиця або розділ програми.</param>
    /// <param name="recordId">Ідентифікатор запису.</param>
    /// <param name="summary">Короткий опис.</param>
    /// <param name="detailsJson">Деталі у форматі JSON (необов'язково).</param>
    public Task<long> LogAsync(string eventType, string table, string recordId, string summary, string? detailsJson = null) =>
        db.ExecuteScalarAsync<long>("SELECT fn_log_event(@type, @table, @record, @summary, @details::jsonb)",
            P("type", eventType), P("table", table), P("record", recordId), P("summary", summary),
            P("details", detailsJson));
}
