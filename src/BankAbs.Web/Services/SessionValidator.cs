using System.Data.Common;
using System.Globalization;
using System.Security.Claims;
using BankAbs.Data.Infrastructure;
using BankAbs.Data.Repositories;
using BankAbs.Domain.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BankAbs.Web.Services;

/// <summary>
/// Перевірка сеансу під час кожного запиту:
/// <list type="bullet">
/// <item>заблокований адміністратором користувач одразу втрачає доступ;</item>
/// <item>зміна ролі або прав ролі застосовується без повторного входу;</item>
/// <item>сеанс завершується після бездіяльності довше за параметр security.session_minutes.</item>
/// </list>
/// </summary>
public static class SessionValidator
{
    /// <summary>Обробник події CookieAuthenticationEvents.OnValidatePrincipal.</summary>
    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var principal = context.Principal;
        if (principal is null || !int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var userId))
        {
            context.RejectPrincipal();
            return;
        }

        var dataSource = context.HttpContext.RequestServices.GetRequiredService<DbDataSource>();
        var db = new Database(dataSource, SystemUserContext.Instance);
        var parameters = new ParameterRepository(db);
        var users = new UserRepository(db);

        var sessionMinutes = await parameters.GetIntAsync("security.session_minutes", 30);
        var lastActivity = context.Properties.IssuedUtc ?? DateTimeOffset.UtcNow;
        if (DateTimeOffset.UtcNow - lastActivity > TimeSpan.FromMinutes(sessionMinutes))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }

        var user = await users.GetAsync(userId);
        if (user is not { IsActive: true })
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }

        var permissions = await users.PermissionsAsync(user.RoleCode);
        var current = principal.FindAll(Permissions.ClaimType).Select(c => c.Value).Order(StringComparer.Ordinal);
        if (user.RoleCode != principal.FindFirstValue(ClaimTypes.Role) || !current.SequenceEqual(permissions.Order(StringComparer.Ordinal)))
        {
            var identity = new ClaimsIdentity(principal.Claims.Where(c =>
                c.Type != Permissions.ClaimType && c.Type != ClaimTypes.Role && c.Type != CurrentUserContext.RoleNameClaimType),
                CookieAuthenticationDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim(ClaimTypes.Role, user.RoleCode));
            identity.AddClaim(new Claim(CurrentUserContext.RoleNameClaimType, user.RoleName));
            identity.AddClaims(permissions.Select(p => new Claim(Permissions.ClaimType, p)));
            context.ReplacePrincipal(new ClaimsPrincipal(identity));
        }

        // Ковзний строк сеансу: кожен запит продовжує сеанс на security.session_minutes
        context.Properties.IssuedUtc = DateTimeOffset.UtcNow;
        context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(sessionMinutes);
        context.ShouldRenew = true;
    }
}
