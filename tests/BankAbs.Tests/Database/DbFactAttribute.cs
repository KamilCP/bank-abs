namespace BankAbs.Tests.Database;

/// <summary>
/// Інтеграційний тест серверної частини БД. Виконується, якщо задано змінну середовища
/// BANKABS_TEST_CONNECTION з рядком підключення до бази, створеної скриптами db/*.sql;
/// інакше тест пропускається.
/// </summary>
public sealed class DbFactAttribute : FactAttribute
{
    public const string VariableName = "BANKABS_TEST_CONNECTION";

    public DbFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(VariableName)))
        {
            Skip = $"Не задано змінну середовища {VariableName}";
        }
    }
}
