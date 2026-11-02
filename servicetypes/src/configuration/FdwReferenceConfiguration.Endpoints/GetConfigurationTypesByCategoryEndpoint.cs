using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Fdw.Operations.Endpoints.ConfigurationMetadata;

namespace FdwReferenceConfiguration.Endpoints;

/// <summary>
/// Concrete endpoint to list configuration types by category.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetConfigurationTypesByCategoryEndpoint : GetConfigurationTypesByCategoryEndpointBase
{
    /// <inheritdoc />
    public GetConfigurationTypesByCategoryEndpoint(IConfigurationContainerLookup containerLookup, ILogger<GetConfigurationTypesByCategoryEndpoint> logger)
        : base(logger, containerLookup) {
    }
}
