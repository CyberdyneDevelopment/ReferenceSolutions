using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration.Endpoints;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConfiguration.Endpoints;

/// <summary>
/// Endpoint to list configuration instances with optional category filter.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Static ConfigurationTypes dependency requires integration testing")]
public class ListConfigurationInstancesEndpoint : ListConfigurationInstancesEndpointBase
{
    /// <inheritdoc />
    public ListConfigurationInstancesEndpoint(
        IConfigurationGatewayProvider configurationGateways,
        IConfigurationContainerLookup containerLookup,
        ILogger<ListConfigurationInstancesEndpoint> logger)
        : base(configurationGateways, containerLookup, logger)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Configuration");
    }
}
