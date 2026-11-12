using System.Data.Common;
using System.Globalization;
using System.Security.Claims;
using BankAbs.Data.Infrastructure;
using BankAbs.Data.Repositories;
using BankAbs.Domain.Entities;
using BankAbs.Domain.Security;

namespace BankAbs.Web.Services;

/// <summary>Результат спроби входу.</summary>
/// <param name="Principal">Користувач із правами (якщо вхід успішний).</param>
/// <param name="Error">Повідомлення про помилку для форми входу.</param>
public sealed record LoginResult(ClaimsPrincipal? Principal, string? Error)
{
    /// <summary>Чи успішний вхід.</summary>
    public bool Succeeded => Principal is not null;
}

/// <summary>
/// Перевірка логіна і пароля, облік невдалих спроб і тимчасове блокування,
/// формування набору claim (роль, права) і запис подій входу в журнал.
/// </summary>
public sealed class LoginService(DbDataSource dataSource)
{
    private const string InvalidCredentials = "Невірний логін або пароль";

    /// <summary>Перевіряє облікові дані.</summary>
    /// <param name="login">Логін.</param>
    /// <param name="password">Пароль.</param>
    /// <param name="ipAddress">IP-адреса клієнта.</param>
    public async Task<LoginResult> SignInAsync(string? login, string? password, string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrEmpty(password))
        {
            return new LoginResult(null, "Введіть логін і пароль");
        }

        var anonymousDb = new Database(dataSource, new FixedUserContext(null, login.Trim(), ipAddress));
        var users = new UserRepository(anonymousDb);
        var parameters = new ParameterRepository(anonymousDb);
        var audit = new AuditRepository(anonymousDb);

        var user = await users.FindByLoginAsync(login);
        if (user is null)
        {
            await audit.LogAsync(AuditEventTypes.LoginFailed, "app_user", string.Empty, "Невідомий логін: " + login.Trim());
            return new LoginResult(null, InvalidCredentials);
        }

        var userDb = new Database(dataSource, new FixedUserContext(user.Id, user.Login, ipAddress));
        var userAudit = new AuditRepository(userDb);
        var userRepository = new UserRepository(userDb);
        var recordId = user.Id.ToString(CultureInfo.InvariantCulture);

        if (!user.IsActive)
        {
            await userAudit.LogAsync(AuditEventTypes.LoginFailed, "app_user", recordId, "Спроба входу заблокованого користувача");
            return new LoginResult(null, "Обліковий запис заблоковано. Зверніться до адміністратора");
        }

        if (user.LockedUntil is { } lockedUntil && lockedUntil > DateTime.UtcNow)
        {
            await userAudit.LogAsync(AuditEventTypes.LoginFailed, "app_user", recordId, "Спроба входу під час тимчасового блокування");
            return new LoginResult(null, $"Вхід тимчасово заблоковано до {Fmt.DateTime(lockedUntil)} через невдалі спроби");
        }

        if (!PasswordHasher.Verify(password, user.PasswordHash))
        {
            var maxAttempts = await parameters.GetIntAsync("security.max_failed_logins", 5);
            var lockMinutes = await parameters.GetIntAsync("security.lockout_minutes", 15);
            var locked = await userRepository.RegisterFailedLoginAsync(user.Id, maxAttempts, lockMinutes);
            await userAudit.LogAsync(AuditEventTypes.LoginFailed, "app_user", recordId,
                locked ? $"Невірний пароль; вхід заблоковано на {lockMinutes} хв" : "Невірний пароль");
            return new LoginResult(null, locked
                ? $"Невірний пароль. Вхід заблоковано на {lockMinutes} хв"
                : InvalidCredentials);
        }

        await userRepository.RegisterSuccessfulLoginAsync(user.Id);
        await userAudit.LogAsync(AuditEventTypes.Login, "app_user", recordId, "Вхід у систему");
        var permissions = await users.PermissionsAsync(user.RoleCode);
        return new LoginResult(CreatePrincipal(user, permissions, ipAddress), null);
    }

    private static ClaimsPrincipal CreatePrincipal(AppUser user, IEnumerable<string> permissions, string ipAddress)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, user.Login),
            new(ClaimTypes.GivenName, user.FullName),
            new(ClaimTypes.Role, user.RoleCode),
            new(CurrentUserContext.RoleNameClaimType, user.RoleName),
            new(CurrentUserContext.IpClaimType, ipAddress),
        };
        claims.AddRange(permissions.Select(p => new Claim(Permissions.ClaimType, p)));
        var identity = new ClaimsIdentity(claims, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    private sealed record FixedUserContext(int? UserId, string Login, string IpAddress) : IUserContext;
}
