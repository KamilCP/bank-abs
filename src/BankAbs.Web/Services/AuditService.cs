using BankAbs.Data.Repositories;
using BankAbs.Domain.Entities;

namespace BankAbs.Web.Services;

/// <summary>
/// Запис у журнал подій рівня інтерфейсу: відкриття таблиць (з урахуванням параметра
/// ui.log_table_views), формування звітів, службові дії.
/// Зміни даних записують тригери БД, тому тут вони не дублюються.
/// </summary>
public sealed class AuditService(AuditRepository audit, ParameterRepository parameters, ILogger<AuditService> logger)
{
    private bool? _logViews;

    /// <summary>Фіксує відкриття таблиці або списку.</summary>
    public async Task TableViewedAsync(string table, string title)
    {
        _logViews ??= await parameters.GetBoolAsync("ui.log_table_views", true);
        if (_logViews == true)
        {
            await SafeLogAsync(AuditEventTypes.View, table, string.Empty, "Відкрито таблицю «" + title + "»");
        }
    }

    /// <summary>Фіксує формування звіту.</summary>
    public Task ReportCreatedAsync(string report, string parametersText) =>
        SafeLogAsync(AuditEventTypes.Report, "report", report, $"Сформовано звіт «{report}» ({parametersText})");

    /// <summary>Фіксує службову дію.</summary>
    public Task ActionAsync(string table, string recordId, string summary) =>
        SafeLogAsync(AuditEventTypes.Action, table, recordId, summary);

    private async Task SafeLogAsync(string type, string table, string record, string summary)
    {
        try
        {
            await audit.LogAsync(type, table, record, summary);
        }
        catch (System.Data.Common.DbException ex)
        {
            // Помилка журналювання не повинна переривати роботу користувача
            logger.LogWarning(ex, "Не вдалося записати подію {Type} у журнал", type);
        }
    }
}
