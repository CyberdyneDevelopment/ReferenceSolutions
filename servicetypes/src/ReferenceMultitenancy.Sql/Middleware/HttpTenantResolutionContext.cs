using System;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.AspNetCore.Http;

namespace ReferenceMultitenancy.Sql.Middleware;

/// <summary>
/// Context for resolving tenant from HTTP request.
/// </summary>
public sealed class HttpTenantResolutionContext : ITenantResolutionContext
{
    /// <summary>Initializes a new instance from an HTTP context.</summary>
    public HttpTenantResolutionContext(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Host = context.Request.Host.Value;
        TenantHeader = context.Request.Headers["X-Tenant-Id"].ToString();

        // RouteSlug is not supported in this implementation.
        // Use X-Tenant-Id header or JWT claims instead.
        RouteSlug = null;

        var tenantClaim = context.User.FindFirst(ClaimDefinitions.tenantId.Name)?.Value;
        if (!string.IsNullOrEmpty(tenantClaim) && Guid.TryParse(tenantClaim, out var tenantId))
        {
            ClaimsTenantId = tenantId;
        }
    }

    /// <inheritdoc />
    public string? Host { get; }

    /// <inheritdoc />
    public string? TenantHeader { get; }

    /// <inheritdoc />
    public string? RouteSlug { get; }

    /// <inheritdoc />
    public Guid? ClaimsTenantId { get; }
}
