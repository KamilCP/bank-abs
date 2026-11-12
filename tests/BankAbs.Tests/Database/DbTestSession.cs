using System.Data.Common;
using System.Globalization;
using Npgsql;

namespace BankAbs.Tests.Database;

/// <summary>
/// Підключення до тестової бази з відкритою транзакцією: усі зміни тесту відкочуються
/// під час звільнення об'єкта, тому тести не впливають на дані і один на одного.
/// </summary>
public sealed class DbTestSession : IAsyncDisposable
{
    private readonly NpgsqlConnection _connection;
    private readonly DbTransaction _transaction;

    private DbTestSession(NpgsqlConnection connection, DbTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public static async Task<DbTestSession> StartAsync(int? userId = 1)
    {
        var dataSource = NpgsqlDataSource.Create(Environment.GetEnvironmentVariable(DbFactAttribute.VariableName)!);
        var connection = (NpgsqlConnection)await dataSource.OpenConnectionAsync();
        var transaction = await connection.BeginTransactionAsync();
        var session = new DbTestSession(connection, transaction);
        if (userId is not null)
        {
            await session.ExecuteAsync("SELECT set_config('bankabs.user_id', @id, true), set_config('bankabs.user_login', 'test', true)",
                ("id", userId.Value.ToString(CultureInfo.InvariantCulture)));
        }

        return session;
    }

    public async Task<T> ScalarAsync<T>(string sql, params (string Name, object? Value)[] parameters)
    {
        await using var command = Create(sql, parameters);
        var value = await command.ExecuteScalarAsync();
        return (T)Convert.ChangeType(value!, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T), CultureInfo.InvariantCulture);
    }

    public async Task<int> ExecuteAsync(string sql, params (string Name, object? Value)[] parameters)
    {
        await using var command = Create(sql, parameters);
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<List<object[]>> RowsAsync(string sql, params (string Name, object? Value)[] parameters)
    {
        await using var command = Create(sql, parameters);
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<object[]>();
        while (await reader.ReadAsync())
        {
            var values = new object[reader.FieldCount];
            reader.GetValues(values);
            rows.Add(values);
        }

        return rows;
    }

    /// <summary>Очікує помилку сервера БД і повертає її текст.</summary>
    public async Task<string> ErrorAsync(string sql, params (string Name, object? Value)[] parameters)
    {
        await ExecuteAsync("SAVEPOINT expected_error");
        try
        {
            await ExecuteAsync(sql, parameters);
        }
        catch (DbException ex)
        {
            await ExecuteAsync("ROLLBACK TO SAVEPOINT expected_error");
            return ex.Message;
        }

        throw new InvalidOperationException("Очікувалася помилка сервера БД: " + sql);
    }

    public async ValueTask DisposeAsync()
    {
        await _transaction.RollbackAsync();
        await _connection.DisposeAsync();
    }

    private DbCommand Create(string sql, (string Name, object? Value)[] parameters)
    {
        var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = _transaction;
        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        return command;
    }
}
