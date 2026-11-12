using System.Data.Common;
using System.Reflection;

namespace BankAbs.Data.Infrastructure;

/// <summary>
/// Створення об'єктів БД під час першого запуску: якщо в базі немає таблиці schema_version,
/// послідовно виконуються вбудовані скрипти 01_schema … 04_seed в одній транзакції.
/// </summary>
public sealed class DatabaseInitializer(DbDataSource dataSource)
{
    private const string ResourcePrefix = "BankAbs.Data.Sql.";

    private static readonly string[] ScriptNames =
        ["01_schema.sql", "02_functions.sql", "03_triggers.sql", "04_seed.sql"];

    /// <summary>Створює схему і початкові дані, якщо їх ще немає.</summary>
    /// <returns><c>true</c>, якщо база була створена під час цього виклику.</returns>
    public async Task<bool> EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using (var check = connection.CreateCommand())
        {
            check.CommandText = "SELECT to_regclass('public.schema_version') IS NOT NULL";
            var exists = (bool)(await check.ExecuteScalarAsync(cancellationToken))!;
            if (exists)
            {
                return false;
            }
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        foreach (var script in ScriptNames)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = ReadScript(script);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    /// <summary>Текст вбудованого SQL-скрипту.</summary>
    public static string ReadScript(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourcePrefix + name)
                           ?? throw new InvalidOperationException("Не знайдено вбудований скрипт " + name);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
