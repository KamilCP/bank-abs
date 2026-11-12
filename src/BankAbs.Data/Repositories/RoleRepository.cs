using BankAbs.Data.Infrastructure;
using BankAbs.Domain.Entities;
using static BankAbs.Data.Infrastructure.Database;

namespace BankAbs.Data.Repositories;

/// <summary>Ролі і матриця прав доступу.</summary>
public sealed class RoleRepository(Database db)
{
    /// <summary>Усі ролі з кількістю користувачів.</summary>
    public Task<List<AppRole>> RolesAsync() =>
        db.QueryAsync("""
            SELECT r.*, (SELECT count(*) FROM app_user u WHERE u.role_code = r.code)::integer AS user_count
              FROM app_role r ORDER BY r.is_system DESC, r.name
            """, r => new AppRole(r.Get<string>("code"), r.Get<string>("name"), r.Get<string>("description"),
            r.Get<bool>("is_system"), r.Get<int>("user_count")));

    /// <summary>Усі функції системи.</summary>
    public Task<List<AppPermission>> PermissionsAsync() =>
        db.QueryAsync("SELECT * FROM app_permission ORDER BY sort_order, code",
            r => new AppPermission(r.Get<string>("code"), r.Get<string>("name"), r.Get<string>("module")));

    /// <summary>Пари «роль — право».</summary>
    public async Task<HashSet<(string Role, string Permission)>> GrantsAsync()
    {
        var rows = await db.QueryAsync("SELECT role_code, permission_code FROM role_permission",
            r => (r.GetString(0), r.GetString(1)));
        return [.. rows];
    }

    /// <summary>
    /// Зберігає набір прав ролі: видаляє зняті і додає нові права в одній транзакції.
    /// Права адміністрування для ролі admin не знімаються (захист від втрати доступу).
    /// </summary>
    public Task<int> SetPermissionsAsync(string roleCode, IReadOnlyCollection<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);
        var codes = permissions.ToArray();
        return db.InTransactionAsync(async (connection, transaction) =>
        {
            await using var delete = CreateCommand(connection, transaction, """
                DELETE FROM role_permission
                 WHERE role_code = @role AND NOT (permission_code = ANY(@codes))
                   AND NOT (role_code = 'admin' AND permission_code IN ('admin.users', 'admin.roles'))
                """, P("role", roleCode), P("codes", codes));
            var removed = await delete.ExecuteNonQueryAsync();
            await using var insert = CreateCommand(connection, transaction, """
                INSERT INTO role_permission (role_code, permission_code)
                SELECT @role, code FROM app_permission WHERE code = ANY(@codes)
                ON CONFLICT DO NOTHING
                """, P("role", roleCode), P("codes", codes));
            var added = await insert.ExecuteNonQueryAsync();
            return removed + added;
        });
    }
}
