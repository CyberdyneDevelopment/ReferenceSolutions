using System.Diagnostics.CodeAnalysis;
using System;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints;

/// <summary>
/// Endpoint to get a specific tenant by ID.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetTenantEndpoint : GetTenantEndpointBase
{
    private readonly ILogger<GetTenantEndpoint> _logger;

    /// <inheritdoc />
    public GetTenantEndpoint(
        ITenantProvider tenantProvider,
        ISystemRoleConfiguration systemRoleConfiguration,
        ILogger<GetTenantEndpoint> logger)
        : base(tenantProvider, systemRoleConfiguration)
    {
        _logger = logger;
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
    protected override void OnAccessDenied(string userId, Guid tenantId)
    {
        Logging.TenantLog.UserTenantAccessDenied(_logger, userId, tenantId);
    }
}
