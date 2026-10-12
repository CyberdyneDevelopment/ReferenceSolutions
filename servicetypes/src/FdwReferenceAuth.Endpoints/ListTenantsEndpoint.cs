using Fdw.Services.Users.Configuration;
﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints;
using Fdw.Services.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint to list all tenants the user has access to.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListTenantsEndpoint : ListTenantsEndpointBase
{
    private readonly UserTenantConfigurationProvider _userTenantProvider;
    private readonly ILogger<ListTenantsEndpoint> _logger;

    /// <inheritdoc />
    public ListTenantsEndpoint(ITenantProvider tenantProvider, ISystemRoleConfiguration systemRoleConfiguration, UserTenantConfigurationProvider userTenantProvider, ILogger<ListTenantsEndpoint>? logger)
        : base(logger ?? NullLogger<ListTenantsEndpoint>.Instance, tenantProvider, systemRoleConfiguration) {
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
            var allResult = includeInactive
                ? await TenantProvider.GetAllTenants(ct).ConfigureAwait(false)
                : await TenantProvider.GetActiveTenants(ct).ConfigureAwait(false);
            // Why filtered here rather than in ITenantProvider: GetAllTenants/GetActiveTenants
            // are general-purpose reads other callers legitimately need unfiltered (e.g. an
            // admin config screen managing every tenant row). IsSelectable is specifically a
            // list/switch-UI concern -- structural sentinel tenants (default, system) are real
            // rows admins can still see and edit elsewhere, just never a place anyone operates
            // "as", admin included.
            return allResult.IsSuccess && allResult.Value is not null
                ? GenericResult<IEnumerable<ITenant>>.Success(allResult.Value.Where(t => t.IsSelectable))
                : allResult;
        }

        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return GenericResult<IEnumerable<ITenant>>.Success(Array.Empty<ITenant>());
        }

        // The user's memberships, then each tenant they name.
        var memberships = await _userTenantProvider
            .Find<IUserTenantImplementationConfiguration>(t => t.UserId == userGuid, ct)
            .ConfigureAwait(false);
        if (!memberships.IsSuccess || memberships.Value is null)
        {
            // Why: Fail-loud — a store failure should surface, not silently return an empty list.
            return memberships.ToNewResult<IEnumerable<ITenant>>();
        }

        var tenants = new List<ITenant>(memberships.Value.Count);
        foreach (var membership in memberships.Value)
        {
            var tenantResult = await TenantProvider.GetTenant(membership.TenantId, ct).ConfigureAwait(false);
            // A failed read is passed on; only a tenant that genuinely no longer exists is skipped.
            if (!tenantResult.IsSuccess)
                return tenantResult.ToNewResult<IEnumerable<ITenant>>();
            if (tenantResult.Value is null)
                continue;
            var tenant = tenantResult.Value;
            if (!includeInactive && !tenant.IsActive)
                continue;
            // Structural sentinel tenants (default, system) never appear in a switch list,
            // even when the caller happens to hold a membership row in one (e.g. the
            // default-tenant safety-net membership seeded for every user).
            if (!tenant.IsSelectable)
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

        // Why null on a failed read: the base declares Task<Guid?>, so a failure has nowhere to go. That
        // base should return a result instead; until it does, this is the one place a failure is absorbed.
        var result = await _userTenantProvider
            .Find<IUserTenantImplementationConfiguration>(t => t.UserId == userGuid && t.IsDefault, ct)
            .ConfigureAwait(false);
        return result.IsSuccess && result.Value is { Count: > 0 } defaults ? defaults[0].TenantId : null;
    }
}
