using System.Data.Common;
using BankAbs.Domain.Common;

namespace BankAbs.Data.Infrastructure;

/// <summary>Параметр SQL-запиту (у тексті запиту позначається як <c>@name</c>).</summary>
/// <param name="Name">Назва параметра без символу @.</param>
/// <param name="Value">Значення; <c>null</c> передається як SQL NULL.</param>
public readonly record struct SqlParam(string Name, object? Value);

/// <summary>
/// Виконання SQL-запитів через <see cref="DbDataSource"/>. Кожна операція зміни даних
/// виконується в окремій транзакції, на початку якої серверу передається контекст
/// користувача для тригерів аудиту. Помилки бізнес-правил перетворюються на
/// <see cref="BusinessRuleException"/>.
/// </summary>
public sealed class Database(DbDataSource dataSource, IUserContext userContext)
{
    private const string SetContextSql =
        "SELECT set_config('bankabs.user_id', @ctx_user_id, true), set_config('bankabs.user_login', @ctx_login, true), "
        + "set_config('bankabs.ip', @ctx_ip, true)";

    /// <summary>Створює параметр запиту.</summary>
    public static SqlParam P(string name, object? value) => new(name, value);

    /// <summary>Виконує запит і перетворює кожен рядок результату функцією <paramref name="map"/>.</summary>
    public async Task<List<T>> QueryAsync<T>(string sql, Func<DbDataReader, T> map, params SqlParam[] parameters)
    {
        ArgumentNullException.ThrowIfNull(map);
        return await RunAsync(async connection =>
        {
            await using var command = CreateCommand(connection, null, sql, parameters);
            await using var reader = await command.ExecuteReaderAsync();
            var result = new List<T>();
            while (await reader.ReadAsync())
            {
                result.Add(map(reader));
            }

            return result;
        });
    }

    /// <summary>Повертає перший рядок результату або <c>null</c>.</summary>
    public async Task<T?> QueryFirstAsync<T>(string sql, Func<DbDataReader, T> map, params SqlParam[] parameters)
        where T : class
    {
        var rows = await QueryAsync(sql, map, parameters);
        return rows.Count > 0 ? rows[0] : null;
    }

    /// <summary>Повертає значення першої колонки першого рядка.</summary>
    public async Task<T> ScalarAsync<T>(string sql, params SqlParam[] parameters)
    {
        return await RunAsync(async connection =>
        {
            await using var command = CreateCommand(connection, null, sql, parameters);
            var value = await command.ExecuteScalarAsync();
            return value is null or DBNull ? default! : (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T),
                System.Globalization.CultureInfo.InvariantCulture);
        });
    }

    /// <summary>Виконує команду зміни даних у транзакції з контекстом користувача.</summary>
    /// <returns>Кількість змінених рядків.</returns>
    public Task<int> ExecuteAsync(string sql, params SqlParam[] parameters) =>
        InTransactionAsync(async (connection, transaction) =>
        {
            await using var command = CreateCommand(connection, transaction, sql, parameters);
            return await command.ExecuteNonQueryAsync();
        });

    /// <summary>Виконує запит, що змінює дані і повертає одне значення (наприклад, виклик fn_*).</summary>
    public Task<T> ExecuteScalarAsync<T>(string sql, params SqlParam[] parameters) =>
        InTransactionAsync(async (connection, transaction) =>
        {
            await using var command = CreateCommand(connection, transaction, sql, parameters);
            var value = await command.ExecuteScalarAsync();
            return value is null or DBNull ? default! : (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T),
                System.Globalization.CultureInfo.InvariantCulture);
        });

    /// <summary>
    /// Виконує кілька команд в одній транзакції з контекстом користувача.
    /// Якщо <paramref name="work"/> завершується винятком, транзакція відкочується.
    /// </summary>
    public Task<T> InTransactionAsync<T>(Func<DbConnection, DbTransaction, Task<T>> work)
    {
        ArgumentNullException.ThrowIfNull(work);
        return RunAsync(async connection =>
        {
            await using var transaction = await connection.BeginTransactionAsync();
            await using (var context = CreateCommand(connection, transaction, SetContextSql,
                             P("ctx_user_id", userContext.UserId?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty),
                             P("ctx_login", userContext.Login),
                             P("ctx_ip", userContext.IpAddress)))
            {
                await context.ExecuteNonQueryAsync();
            }

            var result = await work(connection, transaction);
            await transaction.CommitAsync();
            return result;
        });
    }

    /// <summary>Створює команду з параметрами.</summary>
    public static DbCommand CreateCommand(DbConnection connection, DbTransaction? transaction, string sql,
        params SqlParam[] parameters)
    {
        ArgumentNullException.ThrowIfNull(connection);
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        foreach (var parameter in parameters)
        {
            var dbParameter = command.CreateParameter();
            dbParameter.ParameterName = parameter.Name;
            dbParameter.Value = parameter.Value ?? DBNull.Value;
            command.Parameters.Add(dbParameter);
        }

        return command;
    }

    private async Task<T> RunAsync<T>(Func<DbConnection, Task<T>> action)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync();
            return await action(connection);
        }
        catch (DbException ex)
        {
            var translated = DbErrorTranslator.Translate(ex);
            if (translated is not null)
            {
                throw translated;
            }

            throw;
        }
    }
}
