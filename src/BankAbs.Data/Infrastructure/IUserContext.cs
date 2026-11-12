namespace BankAbs.Data.Infrastructure;

/// <summary>
/// Відомості про поточного користувача програми. Передаються на сервер БД на початку кожної
/// транзакції (параметри сеансу bankabs.*) і використовуються тригерами аудиту.
/// </summary>
public interface IUserContext
{
    /// <summary>Ідентифікатор користувача або <c>null</c>, якщо вхід не виконано.</summary>
    int? UserId { get; }

    /// <summary>Логін користувача.</summary>
    string Login { get; }

    /// <summary>IP-адреса клієнта.</summary>
    string IpAddress { get; }
}

/// <summary>Контекст системних дій без користувача (ініціалізація БД, тести).</summary>
public sealed class SystemUserContext : IUserContext
{
    /// <summary>Спільний екземпляр.</summary>
    public static SystemUserContext Instance { get; } = new();

    /// <inheritdoc />
    public int? UserId => null;

    /// <inheritdoc />
    public string Login => "system";

    /// <inheritdoc />
    public string IpAddress => string.Empty;
}
