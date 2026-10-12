using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Fdw.Operations.Endpoints.ConfigurationMetadata;

namespace FdwReferenceConfiguration.Endpoints;

/// <summary>
/// Concrete endpoint to list configuration categories.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetConfigurationCategoriesEndpoint : GetConfigurationCategoriesEndpointBase
{
    /// <inheritdoc />
    public GetConfigurationCategoriesEndpoint(IConfigurationContainerLookup containerLookup, ILogger<GetConfigurationCategoriesEndpoint> logger)
        : base(logger, containerLookup) {
    }
}
