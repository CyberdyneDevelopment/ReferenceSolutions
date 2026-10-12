using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;
using Fdw.Services.Data.Transformations;

namespace FdwReferenceTransformations.Endpoints;

/// <summary>
/// Updates the ordinal positions of transforms in a field mapping's transform chain.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ReorderFieldMappingTransformsEndpoint : ReorderFieldMappingTransformsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="ReorderFieldMappingTransformsEndpoint"/>.
    /// </summary>
    public ReorderFieldMappingTransformsEndpoint(IConfigurationGatewayProvider configurationGateways, TransformationConfigurationProvider transformations, ILogger<ReorderFieldMappingTransformsEndpoint> logger)
        : base(logger, configurationGateways, transformations) {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Transformations");
    }
}
