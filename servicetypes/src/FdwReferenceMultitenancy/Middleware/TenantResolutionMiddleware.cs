using System;
using System.Threading.Tasks;
using Fdw.Services.Multitenancy.Abstractions;
using FdwReferenceMultitenancy.Logging;
using Fdw.Claims.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceMultitenancy.Middleware;

/// <summary>
/// Middleware that makes the tenant the caller's token was issued for the request's tenant context.
/// </summary>
/// <remarks>
/// The token's tenant is the ONE active tenant (FDW-825): row-level security reads it from the
/// authentication context, and this makes the same tenant available as an
/// <see cref="ITenantContext"/>. There is no second way to set it — the <c>X-Tenant-Id</c> header
/// this used to honour changed this context and nothing the database saw, so a caller could appear
/// to act in a tenant while reading another's rows. Switching tenant is a new token
/// (<c>POST /tenants/switch</c>).
/// </remarks>
public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    /// <summary>Initializes a new instance of the <see cref="TenantResolutionMiddleware"/> class.</summary>
    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(next);

        _next = next;
        _logger = logger ?? NullLogger<TenantResolutionMiddleware>.Instance;
    }

    /// <summary>Processes the request to resolve and set the tenant context.</summary>
    public async Task Invoke(
        HttpContext context,
        IMutableTenantContext tenantContext,
        ITenantProvider tenantProvider)
    {
        // The token's tenant is the only source.
        var tenantClaim = context.User.FindFirst(ClaimDefinitions.tenantId.Name)?.Value;

        if (!string.IsNullOrEmpty(tenantClaim) && Guid.TryParse(tenantClaim, out var tenantId))
        {
            var denied = await ResolveFromJwtClaim(context, tenantId, tenantContext, tenantProvider).ConfigureAwait(false);
            if (denied)
            {
                return;
            }
        }

        await _next(context).ConfigureAwait(false);
    }

    /// <summary>Resolves the tenant from a JWT claim tenant_id value.</summary>
    /// <returns><c>true</c> if access was denied and the response should short-circuit.</returns>
    private async Task<bool> ResolveFromJwtClaim(
        HttpContext context,
        Guid tenantId,
        IMutableTenantContext tenantContext,
        ITenantProvider tenantProvider)
    {
        // Why: propagate the request's cancellation token instead of default so this lookup is
        // cancelled with the request rather than running to completion after the client disconnects.
        var result = await tenantProvider.GetTenant(tenantId, context.RequestAborted).ConfigureAwait(false);
        // Why: providers can return Success with a null Value when the lookup found no
        // matching tenant (e.g. Guid.Empty or an unknown id). The previous bang-
        // suppression here threw ArgumentNullException inside SetTenant and surfaced
        // as a 500 on every request.
        if (result.IsSuccess && result.Value is not null)
        {
            // No IsSelectable check: that flag tells a UI which tenants to offer, and is not an
            // access rule. Whether the caller may act in this tenant was decided when the token was
            // issued (membership, ITenantProvider.ValidateTenantAccess); what they may see in it is
            // enforced by RLS.
            tenantContext.SetTenant(result.Value);
            TenantMiddlewareLog.ResolvedTenantFromJwtClaim(_logger, tenantId);
        }
        else
        {
            TenantMiddlewareLog.TenantFromJwtClaimNotFound(_logger, tenantId);
        }

        return false;
    }
}
