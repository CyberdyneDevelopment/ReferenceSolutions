using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: switch a feature on or off for a tenant (FDW-1580).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SetTenantFeatureEndpoint : SetTenantFeatureEndpointBase
{
    /// <inheritdoc />
    public SetTenantFeatureEndpoint(
        IFeatureEnablement features,
        IAdministrativeAudit audit,
        ILogger<SetTenantFeatureEndpoint> logger)
        : base(logger, features, audit)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("tenants:write");
        Summary(s =>
        {
            s.Summary = "Switch a feature on or off for a tenant";
            s.Description = "Switches a feature on or off for a tenant. A holder of the tenants:write right in that tenant may; the switch is audited.";
        });
        Tags("Tenants");
    }
}
