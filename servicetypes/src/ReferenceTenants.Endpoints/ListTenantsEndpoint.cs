using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints;
using Fdw.Services.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ReferenceTenants.Endpoints;

/// <summary>
/// Endpoint to list all tenants the user has access to.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListTenantsEndpoint : ListTenantsEndpointBase
{
    private readonly UserTenantConfigurationProvider _userTenantProvider;
    private readonly ILogger<ListTenantsEndpoint> _logger;

    /// <inheritdoc />
    public ListTenantsEndpoint(
        ITenantProvider tenantProvider,
        ISystemRoleConfiguration systemRoleConfiguration,
        UserTenantConfigurationProvider userTenantProvider,
        ILogger<ListTenantsEndpoint>? logger)
        : base(tenantProvider, systemRoleConfiguration)
    {
        _userTenantProvider = userTenantProvider;
        _logger = logger ?? NullLogger<ListTenantsEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
#if DEVELOP
        AllowAnonymous();
#else
        Policies("tenants:read");
#endif
        Tags("Tenants");
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<IEnumerable<ITenant>>> GetTenants(
        string? userId,
        bool isAdmin,
        bool includeInactive,
        CancellationToken ct)
    {
        if (isAdmin)
        {
            return includeInactive
                ? await TenantProvider.GetAllTenants(ct).ConfigureAwait(false)
                : await TenantProvider.GetActiveTenants(ct).ConfigureAwait(false);
        }

        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return GenericResult<IEnumerable<ITenant>>.Success(Array.Empty<ITenant>());
        }

        // Load the user's tenant IDs from the provider, then resolve each tenant.
        var tenantIdsResult = await _userTenantProvider.GetUserTenants(userGuid, ct).ConfigureAwait(false);
        if (!tenantIdsResult.IsSuccess)
        {
            // Why: Fail-loud — a store failure should surface, not silently return an empty list.
            return GenericResult<IEnumerable<ITenant>>.Failure(tenantIdsResult.Messages.ToArray());
        }

        var tenantIds = tenantIdsResult.Value ?? Array.Empty<Guid>();
        var tenants = new List<ITenant>(tenantIds.Count);

        foreach (var tenantId in tenantIds)
        {
            var tenantResult = await TenantProvider.GetTenant(tenantId, ct).ConfigureAwait(false);
            if (!tenantResult.IsSuccess || tenantResult.Value is null)
                continue;

            var tenant = tenantResult.Value;
            if (!includeInactive && !tenant.IsActive)
                continue;

            tenants.Add(tenant);
        }

        return GenericResult<IEnumerable<ITenant>>.Success(tenants);
    }

    /// <inheritdoc />
    protected override async Task<Guid?> GetDefaultTenantId(string userId, CancellationToken ct)
    {
        if (!Guid.TryParse(userId, out var userGuid))
            return null;

        var result = await _userTenantProvider.GetDefaultTenant(userGuid, ct).ConfigureAwait(false);
        return result.IsSuccess ? result.Value : null;
    }
}
