using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Fdw.Operations.Endpoints.ConfigurationMetadata;

namespace FdwReferenceConfiguration.Endpoints;

/// <summary>
/// Concrete endpoint to list root configuration types.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetRootConfigurationTypesEndpoint : GetRootConfigurationTypesEndpointBase
{
    /// <inheritdoc />
    public GetRootConfigurationTypesEndpoint(IConfigurationContainerLookup containerLookup, ILogger<GetRootConfigurationTypesEndpoint> logger)
        : base(logger, containerLookup) {
    }
}
