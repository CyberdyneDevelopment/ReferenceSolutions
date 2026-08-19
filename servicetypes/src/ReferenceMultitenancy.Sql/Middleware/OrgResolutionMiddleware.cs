using System;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using ReferenceMultitenancy.Sql.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ReferenceMultitenancy.Sql.Middleware;

/// <summary>
/// Middleware that resolves the current organization from the JWT <c>orgId</c> claim or
/// <c>X-Org-Id</c> header, falling back to the current tenant's default org
/// (<c>IsDefault=1</c>). Registers after <see cref="TenantResolutionMiddleware"/>.
/// </summary>
public sealed class OrgResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<OrgResolutionMiddleware> _logger;

    /// <summary>Initializes a new instance of <see cref="OrgResolutionMiddleware"/>.</summary>
    public OrgResolutionMiddleware(RequestDelegate next, ILogger<OrgResolutionMiddleware>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(next);

        _next = next;
        _logger = logger ?? NullLogger<OrgResolutionMiddleware>.Instance;
    }

    /// <summary>Processes the request to resolve and set the org context.</summary>
    public async Task Invoke(
        HttpContext context,
        ITenantContext tenantContext,
        IMutableOrgContext orgContext,
        IOrganizationProvider orgProvider)
    {
        // Why: Org resolution requires a tenant context. If tenant resolution didn't run or
        // the request has no tenant (anonymous/public routes), skip silently.
        if (!tenantContext.HasTenant || tenantContext.TenantId is null)
        {
            TenantMiddlewareLog.OrgResolutionSkippedNoTenant(_logger);
            await _next(context).ConfigureAwait(false);
            return;
        }

        var tenantId = tenantContext.TenantId.Value;

        // Priority 1: JWT orgId claim
        var orgClaim = context.User.FindFirst(ClaimDefinitions.orgId.Name)?.Value;
        if (!string.IsNullOrEmpty(orgClaim) && Guid.TryParse(orgClaim, out var claimOrgId))
        {
            var result = await orgProvider.Get(claimOrgId, context.RequestAborted).ConfigureAwait(false);
            if (result.IsSuccess && result.Value is not null)
            {
                orgContext.SetOrg(result.Value);
                TenantMiddlewareLog.ResolvedOrgFromJwtClaim(_logger, claimOrgId);
                await _next(context).ConfigureAwait(false);
                return;
            }

            TenantMiddlewareLog.OrgFromJwtClaimNotFound(_logger, claimOrgId);
        }

        // Priority 2: X-Org-Id header
        var orgHeader = context.Request.Headers["X-Org-Id"].ToString();
        if (!string.IsNullOrEmpty(orgHeader) && Guid.TryParse(orgHeader, out var headerOrgId))
        {
            var result = await orgProvider.Get(headerOrgId, context.RequestAborted).ConfigureAwait(false);
            if (result.IsSuccess && result.Value is not null)
            {
                orgContext.SetOrg(result.Value);
                TenantMiddlewareLog.ResolvedOrgFromHeader(_logger, headerOrgId);
                await _next(context).ConfigureAwait(false);
                return;
            }

            TenantMiddlewareLog.OrgFromHeaderNotFound(_logger, headerOrgId);
        }

        // Priority 3: Tenant's default org (IsDefault=1)
        var defaultResult = await orgProvider.GetDefault(tenantId, context.RequestAborted).ConfigureAwait(false);
        if (defaultResult.IsSuccess && defaultResult.Value is not null)
        {
            orgContext.SetOrg(defaultResult.Value);
            TenantMiddlewareLog.ResolvedDefaultOrg(_logger, defaultResult.Value.Id, tenantId);
        }
        else
        {
            TenantMiddlewareLog.NoDefaultOrgForTenant(_logger, tenantId);
        }

        await _next(context).ConfigureAwait(false);
    }
}
