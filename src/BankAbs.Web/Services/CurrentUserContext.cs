using System.Globalization;
using System.Security.Claims;
using BankAbs.Data.Infrastructure;
using BankAbs.Domain.Security;

namespace BankAbs.Web.Services;

/// <summary>
/// Поточний користувач у межах запиту HTTP або з'єднання Blazor (circuit).
/// Заповнюється проміжним обробником запитів і <see cref="UserCircuitHandler"/>.
/// </summary>
public sealed class CurrentUserContext : IUserContext
{
    /// <summary>Тип claim з IP-адресою, з якої виконано вхід.</summary>
    public const string IpClaimType = "bankabs/ip";

    /// <summary>Тип claim з назвою ролі.</summary>
    public const string RoleNameClaimType = "bankabs/role-name";

    /// <summary>Автентифікований користувач (або анонімний).</summary>
    public ClaimsPrincipal Principal { get; private set; } = new(new ClaimsIdentity());

    /// <inheritdoc />
    public int? UserId =>
        int.TryParse(Principal.FindFirstValue(ClaimTypes.NameIdentifier), NumberStyles.Integer, CultureInfo.InvariantCulture,
            out var id) ? id : null;

    /// <inheritdoc />
    public string Login => Principal.Identity?.Name ?? string.Empty;

    /// <inheritdoc />
    public string IpAddress => Principal.FindFirstValue(IpClaimType) ?? string.Empty;

    /// <summary>ПІБ користувача.</summary>
    public string FullName => Principal.FindFirstValue(ClaimTypes.GivenName) ?? Login;

    /// <summary>Назва ролі.</summary>
    public string RoleName => Principal.FindFirstValue(RoleNameClaimType) ?? string.Empty;

    /// <summary>Чи має користувач право на функцію.</summary>
    public bool Can(string permission) => Principal.HasClaim(Permissions.ClaimType, permission);

    /// <summary>Встановлює користувача.</summary>
    public void Set(ClaimsPrincipal principal) => Principal = principal;
}
