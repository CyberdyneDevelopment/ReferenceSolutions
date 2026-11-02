using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: list the features of a tenant (FDW-1580).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListTenantFeaturesEndpoint : ListTenantFeaturesEndpointBase
{
    /// <inheritdoc />
    public ListTenantFeaturesEndpoint(
        IFeatureEnablement features,
        ILogger<ListTenantFeaturesEndpoint> logger)
        : base(logger, features)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("tenants:read");
        Summary(s =>
        {
            s.Summary = "List the features of a tenant";
            s.Description = "Lists every feature as it stands for a tenant: on, or off with the level that has not switched it on. A holder of the tenants:read right in that tenant may read them.";
        });
        Tags("Tenants");
    }
}
