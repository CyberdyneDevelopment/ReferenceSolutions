using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration.Endpoints;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConfiguration.Endpoints;

/// <summary>
/// Endpoint to get a specific configuration instance with all values.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Static ConfigurationTypes dependency requires integration testing")]
public class GetConfigurationInstanceEndpoint : GetConfigurationInstanceEndpointBase
{
    /// <inheritdoc />
    public GetConfigurationInstanceEndpoint(
        IConfigurationGatewayProvider configurationGateways,
        IConfigurationContainerLookup containerLookup,
        ILogger<GetConfigurationInstanceEndpoint> logger)
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
