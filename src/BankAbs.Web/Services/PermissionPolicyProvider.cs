using BankAbs.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace BankAbs.Web.Services;

/// <summary>
/// Динамічні політики авторизації: політика «perm:код» вимагає claim права з цим кодом.
/// Використання: <c>[Authorize(Policy = "perm:clients.view")]</c> або <c>Permissions.Policy(...)</c>.
/// </summary>
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(Permissions.PolicyPrefix, StringComparison.Ordinal))
        {
            var permission = policyName[Permissions.PolicyPrefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireClaim(Permissions.ClaimType, permission)
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}
