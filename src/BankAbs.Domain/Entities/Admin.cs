namespace BankAbs.Domain.Entities;

/// <summary>Користувач системи.</summary>
public sealed class AppUser
{
    /// <summary>Ідентифікатор.</summary>
    public int Id { get; set; }

    /// <summary>Логін.</summary>
    public string Login { get; set; } = string.Empty;

    /// <summary>ПІБ.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Роль.</summary>
    public string RoleCode { get; set; } = string.Empty;

    /// <summary>Назва ролі.</summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>Активний (може входити в систему).</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Кількість невдалих спроб входу поспіль.</summary>
    public int FailedAttempts { get; set; }

    /// <summary>Заблоковано до (UTC).</summary>
    public DateTime? LockedUntil { get; set; }

    /// <summary>Останній вхід (UTC).</summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>Хеш пароля (лише для перевірки при вході).</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Копія для редагування.</summary>
    public AppUser Clone() => (AppUser)MemberwiseClone();
}

/// <summary>Роль користувача.</summary>
/// <param name="Code">Код ролі.</param>
/// <param name="Name">Назва.</param>
/// <param name="Description">Опис.</param>
/// <param name="IsSystem">Системна роль (не видаляється).</param>
/// <param name="UserCount">Кількість користувачів з цією роллю.</param>
public sealed record AppRole(string Code, string Name, string Description, bool IsSystem, int UserCount);

/// <summary>Функція системи, доступ до якої надається ролям.</summary>
/// <param name="Code">Код права.</param>
/// <param name="Name">Назва.</param>
/// <param name="Module">Модуль.</param>
public sealed record AppPermission(string Code, string Name, string Module);

/// <summary>Запис журналу системи.</summary>
public sealed class AuditEntry
{
    /// <summary>Номер запису.</summary>
    public long Id { get; init; }

    /// <summary>Час події (UTC).</summary>
    public DateTime EventTime { get; init; }

    /// <summary>Логін користувача.</summary>
    public string UserLogin { get; init; } = string.Empty;

    /// <summary>Тип події (<see cref="AuditEventTypes"/>).</summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>Таблиця.</summary>
    public string TableName { get; init; } = string.Empty;

    /// <summary>Ідентифікатор запису.</summary>
    public string RecordId { get; init; } = string.Empty;

    /// <summary>Короткий опис.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>Деталі у форматі JSON.</summary>
    public string? Details { get; init; }

    /// <summary>IP-адреса.</summary>
    public string IpAddress { get; init; } = string.Empty;
}

/// <summary>Системний параметр.</summary>
public sealed class SystemParameter
{
    /// <summary>Ключ.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Значення.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Тип значення: text, int, decimal, bool.</summary>
    public string ValueType { get; set; } = "text";

    /// <summary>Назва для відображення.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Категорія.</summary>
    public string Category { get; set; } = string.Empty;
}
