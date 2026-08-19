using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using ReferenceMultitenancy.Sql.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ReferenceMultitenancy.Sql.Middleware;

/// <summary>
/// Middleware that resolves the current tenant from JWT claims or X-Tenant-Id header
/// and sets the tenant context for the request.
/// </summary>
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
        // Try to resolve tenant from JWT claim first
        var tenantClaim = context.User.FindFirst(ClaimDefinitions.tenantId.Name)?.Value;

        if (!string.IsNullOrEmpty(tenantClaim) && Guid.TryParse(tenantClaim, out var tenantId))
        {
            await ResolveFromJwtClaim(tenantId, tenantContext, tenantProvider, context.RequestAborted).ConfigureAwait(false);
        }
        else
        {
            var tenantHeader = context.Request.Headers["X-Tenant-Id"].ToString();
            if (!string.IsNullOrEmpty(tenantHeader))
            {
                var denied = await ResolveFromHeader(context, tenantHeader, tenantContext, tenantProvider).ConfigureAwait(false);
                if (denied)
                {
                    return;
                }
            }
        }

        await _next(context).ConfigureAwait(false);
    }

    /// <summary>Resolves the tenant from a JWT claim tenant_id value.</summary>
    private async Task ResolveFromJwtClaim(
        Guid tenantId,
        IMutableTenantContext tenantContext,
        ITenantProvider tenantProvider,
        CancellationToken cancellationToken)
    {
        // Why: propagate the request's cancellation token instead of default so this lookup is
        // cancelled with the request rather than running to completion after the client disconnects.
        var result = await tenantProvider.GetTenant(tenantId, cancellationToken).ConfigureAwait(false);
        // Why: providers can return Success with a null Value when the lookup found no
        // matching tenant (e.g. Guid.Empty or an unknown id). The previous bang-
        // suppression here threw ArgumentNullException inside SetTenant and surfaced
        // as a 500 on every request.
        if (result.IsSuccess && result.Value is not null)
        {
            tenantContext.SetTenant(result.Value);
            TenantMiddlewareLog.ResolvedTenantFromJwtClaim(_logger, tenantId);
        }
        else
        {
            TenantMiddlewareLog.TenantFromJwtClaimNotFound(_logger, tenantId);
        }
    }

    /// <summary>Resolves the tenant from the X-Tenant-Id header (GUID or slug).</summary>
    /// <returns><c>true</c> if access was denied and the response should short-circuit.</returns>
    private async Task<bool> ResolveFromHeader(
        HttpContext context,
        string tenantHeader,
        IMutableTenantContext tenantContext,
        ITenantProvider tenantProvider)
    {
        ITenant? resolvedTenant = null;

        // Why: propagate the request's cancellation token instead of default so these lookups are
        // cancelled with the request rather than running to completion after the client disconnects.
        if (Guid.TryParse(tenantHeader, out var headerTenantId))
        {
            var result = await tenantProvider.GetTenant(headerTenantId, context.RequestAborted).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                resolvedTenant = result.Value!;
            }
        }
        else
        {
            var result = await tenantProvider.GetTenantBySlug(tenantHeader, context.RequestAborted).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                resolvedTenant = result.Value!;
            }
        }

        if (resolvedTenant is null)
        {
            return false;
        }

        var userId = context.User.FindFirst("sub")?.Value
            ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? string.Empty;

        var accessResult = await tenantProvider.ValidateTenantAccess(resolvedTenant.Id, userId, context.RequestAborted).ConfigureAwait(false);
        if (!accessResult.IsSuccess)
        {
            TenantMiddlewareLog.TenantAccessCheckFailed(_logger, userId, resolvedTenant.Id);
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            var errorJson = $"{{\"error\":\"Tenant access check failed\",\"tenantId\":\"{resolvedTenant.Id}\"}}";
            await context.Response.WriteAsync(errorJson, context.RequestAborted).ConfigureAwait(false);
            return true;
        }

        if (!accessResult.Value)
        {
            TenantMiddlewareLog.TenantHeaderAccessDenied(_logger, userId, resolvedTenant.Id);
            context.Response.StatusCode = 403;
            return true;
        }

        tenantContext.SetTenant(resolvedTenant);
        if (Guid.TryParse(tenantHeader, out var loggedId))
        {
            TenantMiddlewareLog.ResolvedTenantFromHeader(_logger, loggedId);
        }
        else
        {
            TenantMiddlewareLog.ResolvedTenantBySlug(_logger, tenantHeader);
        }

        return false;
    }
}
