using System.Data.Common;
using BankAbs.Data.Infrastructure;
using BankAbs.Domain.Common;
using BankAbs.Domain.Entities;
using static BankAbs.Data.Infrastructure.Database;

namespace BankAbs.Data.Repositories;

/// <summary>Користувачі системи і облік спроб входу.</summary>
public sealed class UserRepository(Database db)
{
    private const string SelectSql = """
        SELECT u.*, r.name AS role_name, count(*) OVER () AS total_count
          FROM app_user u JOIN app_role r ON r.code = u.role_code
        """;

    private static readonly Dictionary<string, string> SortColumns = new()
    {
        ["login"] = "u.login",
        ["name"] = "u.full_name",
        ["role"] = "r.name",
        ["active"] = "u.is_active",
        ["lastLogin"] = "u.last_login_at",
    };

    /// <summary>Список користувачів.</summary>
    public async Task<PagedResult<AppUser>> ListAsync(ListQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var total = 0;
        var sql = SelectSql + """
             WHERE (@search::text IS NULL OR u.login ILIKE @search OR u.full_name ILIKE @search)
               AND (@kind::text IS NULL OR u.role_code = @kind)
               AND (@status::text IS NULL OR u.is_active = (@status = 'active'))
            """ + SqlSort.OrderBy(SortColumns, query.SortBy, query.Descending, "login", "u.id")
            + " LIMIT @limit OFFSET @offset";
        var items = await db.QueryAsync(sql, r =>
        {
            total = r.Get<int>("total_count");
            return Map(r);
        }, P("search", SqlSort.LikePattern(query.Search)), P("kind", query.Kind), P("status", query.Status),
            P("limit", query.PageSize), P("offset", query.Offset));
        return new PagedResult<AppUser>(items, total, query.Page, query.PageSize);
    }

    /// <summary>Усі користувачі (для фільтра журналу).</summary>
    public Task<List<AppUser>> AllAsync() => db.QueryAsync(SelectSql + " ORDER BY u.login", Map);

    /// <summary>Користувач за логіном (без урахування регістру).</summary>
    public Task<AppUser?> FindByLoginAsync(string login) =>
        db.QueryFirstAsync(SelectSql + " WHERE lower(u.login) = lower(@login)", Map, P("login", login.Trim()));

    /// <summary>Користувач за ідентифікатором.</summary>
    public Task<AppUser?> GetAsync(int id) => db.QueryFirstAsync(SelectSql + " WHERE u.id = @id", Map, P("id", id));

    /// <summary>Коди прав ролі.</summary>
    public Task<List<string>> PermissionsAsync(string roleCode) =>
        db.QueryAsync("SELECT permission_code FROM role_permission WHERE role_code = @role ORDER BY permission_code",
            r => r.GetString(0), P("role", roleCode));

    /// <summary>Створює користувача.</summary>
    /// <returns>Ідентифікатор.</returns>
    public Task<int> CreateAsync(AppUser user, string passwordHash)
    {
        ArgumentNullException.ThrowIfNull(user);
        return db.ExecuteScalarAsync<int>("""
            INSERT INTO app_user (login, full_name, password_hash, role_code, is_active)
            VALUES (lower(@login), @name, @hash, @role, @active) RETURNING id
            """, P("login", user.Login.Trim()), P("name", user.FullName.Trim()), P("hash", passwordHash),
            P("role", user.RoleCode), P("active", user.IsActive));
    }

    /// <summary>Змінює ПІБ, роль і стан користувача.</summary>
    public Task<int> UpdateAsync(AppUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return db.ExecuteAsync("""
            UPDATE app_user SET full_name = @name, role_code = @role, is_active = @active WHERE id = @id
            """, P("name", user.FullName.Trim()), P("role", user.RoleCode), P("active", user.IsActive), P("id", user.Id));
    }

    /// <summary>Встановлює новий пароль і знімає блокування.</summary>
    public Task<int> SetPasswordAsync(int id, string passwordHash) =>
        db.ExecuteAsync("""
            UPDATE app_user SET password_hash = @hash, failed_attempts = 0, locked_until = NULL WHERE id = @id
            """, P("hash", passwordHash), P("id", id));

    /// <summary>Знімає тимчасове блокування після невдалих спроб входу.</summary>
    public Task<int> UnlockAsync(int id) =>
        db.ExecuteAsync("UPDATE app_user SET failed_attempts = 0, locked_until = NULL WHERE id = @id", P("id", id));

    /// <summary>
    /// Збільшує лічильник невдалих спроб; після <paramref name="maxAttempts"/> спроб
    /// блокує вхід на <paramref name="lockMinutes"/> хвилин.
    /// </summary>
    /// <returns><c>true</c>, якщо обліковий запис заблоковано цією спробою.</returns>
    public Task<bool> RegisterFailedLoginAsync(int id, int maxAttempts, int lockMinutes) =>
        db.ExecuteScalarAsync<bool>("""
            UPDATE app_user
               SET failed_attempts = failed_attempts + 1,
                   locked_until = CASE WHEN failed_attempts + 1 >= @max
                                       THEN now() + make_interval(mins => @minutes) ELSE locked_until END
             WHERE id = @id
            RETURNING locked_until IS NOT NULL AND locked_until > now()
            """, P("max", maxAttempts), P("minutes", lockMinutes), P("id", id));

    /// <summary>Фіксує успішний вхід: скидає лічильник спроб, записує час входу.</summary>
    public Task<int> RegisterSuccessfulLoginAsync(int id) =>
        db.ExecuteAsync("""
            UPDATE app_user SET failed_attempts = 0, locked_until = NULL, last_login_at = now() WHERE id = @id
            """, P("id", id));

    private static AppUser Map(DbDataReader r) => new()
    {
        Id = r.Get<int>("id"),
        Login = r.Get<string>("login"),
        FullName = r.Get<string>("full_name"),
        RoleCode = r.Get<string>("role_code"),
        RoleName = r.Get<string>("role_name"),
        IsActive = r.Get<bool>("is_active"),
        FailedAttempts = r.Get<int>("failed_attempts"),
        LockedUntil = r.GetNullable<DateTime>("locked_until"),
        LastLoginAt = r.GetNullable<DateTime>("last_login_at"),
        PasswordHash = r.Get<string>("password_hash"),
    };
}
