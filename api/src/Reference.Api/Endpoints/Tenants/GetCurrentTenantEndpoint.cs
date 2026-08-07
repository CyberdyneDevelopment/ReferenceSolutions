using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints;

namespace Reference.Api.Endpoints;

/// <summary>
/// Endpoint to get the current tenant context.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetCurrentTenantEndpoint : GetCurrentTenantEndpointBase
{
    /// <inheritdoc />
    public GetCurrentTenantEndpoint(
        ITenantProvider tenantProvider,
        ISystemRoleConfiguration systemRoleConfiguration)
        : base(tenantProvider, systemRoleConfiguration)
    {
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
}
