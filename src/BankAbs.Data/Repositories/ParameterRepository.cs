using System.Globalization;
using BankAbs.Data.Infrastructure;
using BankAbs.Domain.Entities;
using static BankAbs.Data.Infrastructure.Database;

namespace BankAbs.Data.Repositories;

/// <summary>Системні параметри.</summary>
public sealed class ParameterRepository(Database db)
{
    /// <summary>Усі параметри.</summary>
    public Task<List<SystemParameter>> ListAsync() =>
        db.QueryAsync("SELECT * FROM system_parameter ORDER BY category, key", r => new SystemParameter
        {
            Key = r.Get<string>("key"),
            Value = r.Get<string>("value"),
            ValueType = r.Get<string>("value_type"),
            Name = r.Get<string>("name"),
            Category = r.Get<string>("category"),
        });

    /// <summary>Значення параметра як рядок.</summary>
    public async Task<string> GetTextAsync(string key, string defaultValue)
    {
        var value = await db.ScalarAsync<string?>("SELECT value FROM system_parameter WHERE key = @key", P("key", key));
        return value ?? defaultValue;
    }

    /// <summary>Значення цілочисельного параметра.</summary>
    public async Task<int> GetIntAsync(string key, int defaultValue) =>
        int.TryParse(await GetTextAsync(key, string.Empty), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)
            ? v : defaultValue;

    /// <summary>Значення числового параметра.</summary>
    public async Task<decimal> GetDecimalAsync(string key, decimal defaultValue) =>
        decimal.TryParse(await GetTextAsync(key, string.Empty), NumberStyles.Number, CultureInfo.InvariantCulture, out var v)
            ? v : defaultValue;

    /// <summary>Значення логічного параметра.</summary>
    public async Task<bool> GetBoolAsync(string key, bool defaultValue) =>
        bool.TryParse(await GetTextAsync(key, string.Empty), out var v) ? v : defaultValue;

    /// <summary>Змінює значення параметра (формат перевіряє тригер БД).</summary>
    public Task<int> UpdateAsync(string key, string value) =>
        db.ExecuteAsync("UPDATE system_parameter SET value = @value WHERE key = @key",
            P("value", value.Trim()), P("key", key));

    /// <summary>Версія сервера БД і назва бази (для форми «Про програму»).</summary>
    public async Task<(string Version, string Database)> ServerInfoAsync()
    {
        var row = await db.QueryFirstAsync("SELECT current_setting('server_version') AS v, current_database() AS d",
            r => new[] { r.GetString(0), r.GetString(1) });
        return (row![0], row[1]);
    }

    /// <summary>Закриття операційного дня (збережена процедура sp_close_day).</summary>
    public Task<int> CloseDayAsync(DateOnly date) =>
        db.ExecuteAsync("CALL sp_close_day(@date)", P("date", date));
}
