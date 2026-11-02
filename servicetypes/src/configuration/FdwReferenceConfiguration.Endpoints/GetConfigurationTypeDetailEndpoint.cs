using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Fdw.Operations.Endpoints.ConfigurationMetadata;

namespace FdwReferenceConfiguration.Endpoints;

/// <summary>
/// Concrete endpoint to get configuration type detail.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetConfigurationTypeDetailEndpoint : GetConfigurationTypeDetailEndpointBase
{
    /// <inheritdoc />
    public GetConfigurationTypeDetailEndpoint(IConfigurationContainerLookup containerLookup, ILogger<GetConfigurationTypeDetailEndpoint> logger)
        : base(logger, containerLookup) {
    }
}
