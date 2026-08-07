using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration.Endpoints;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints;

/// <summary>
/// Endpoint to list configuration instances with optional category filter.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Static ConfigurationTypes dependency requires integration testing")]
public sealed class ListConfigurationInstancesEndpoint : ListConfigurationInstancesEndpointBase
{
    /// <inheritdoc />
    public ListConfigurationInstancesEndpoint(
        IDataGateway dataGateway,
        IConfigurationContainerLookup containerLookup,
        ILogger<ListConfigurationInstancesEndpoint> logger)
        : base(dataGateway, containerLookup, logger)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Configuration");
    }
}
