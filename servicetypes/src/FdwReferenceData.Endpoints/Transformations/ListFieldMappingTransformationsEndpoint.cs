using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;
using Fdw.Services.Data.Transformations;

namespace FdwReferenceTransformations.Endpoints;

/// <summary>
/// Lists all transforms configured for a field mapping.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListFieldMappingTransformationsEndpoint : ListFieldMappingTransformationsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="ListFieldMappingTransformationsEndpoint"/>.
    /// </summary>
    public ListFieldMappingTransformationsEndpoint(IConfigurationGatewayProvider configurationGateways, TransformationConfigurationProvider transformations, ILogger<ListFieldMappingTransformationsEndpoint> logger)
        : base(logger, configurationGateways, transformations) {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Transformations");
    }
}
