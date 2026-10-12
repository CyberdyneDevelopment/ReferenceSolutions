using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Endpoints.ConfigurationMetadata;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConfiguration.Endpoints;

/// <summary>
/// Concrete endpoint to list child configuration types.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetChildConfigurationTypesEndpoint : GetChildConfigurationTypesEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="GetChildConfigurationTypesEndpoint"/> class.</summary>
    public GetChildConfigurationTypesEndpoint(ILogger<GetChildConfigurationTypesEndpoint> logger) : base(logger)
    {
    }

}
