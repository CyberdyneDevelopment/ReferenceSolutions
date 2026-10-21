using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: switch a feature on or off for an organization (FDW-1580).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SetOrganizationFeatureEndpoint : SetOrganizationFeatureEndpointBase
{
    /// <inheritdoc />
    public SetOrganizationFeatureEndpoint(
        IFeatureEnablement features,
        IOrganizationProvider organizations,
        IAdministrativeAudit audit,
        ISystemRoleConfiguration systemRoles,
        ILogger<SetOrganizationFeatureEndpoint> logger)
        : base(logger, features, organizations, audit, systemRoles)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("organizations:write");
        Summary(s =>
        {
            s.Summary = "Switch a feature on or off for an organization";
            s.Description = "Switches a feature on or off for an organization. The tenant of the organization may, for a feature the platform has switched on for the tenant; the switch is audited.";
        });
        Tags("Tenants");
    }
}
