using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: list the features of an organization (FDW-1580).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListOrganizationFeaturesEndpoint : ListOrganizationFeaturesEndpointBase
{
    /// <inheritdoc />
    public ListOrganizationFeaturesEndpoint(
        IFeatureEnablement features,
        IOrganizationProvider organizations,
        ISystemRoleConfiguration systemRoles,
        ILogger<ListOrganizationFeaturesEndpoint> logger)
        : base(logger, features, organizations, systemRoles)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("organizations:read");
        Summary(s =>
        {
            s.Summary = "List the features of an organization";
            s.Description = "Lists every feature as it stands for an organization: on, or off with the level that has not switched it on.";
        });
        Tags("Tenants");
    }
}
