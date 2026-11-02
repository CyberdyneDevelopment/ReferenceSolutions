using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;
using Fdw.Services.Data.Transformations;

namespace FdwReferenceTransformations.Endpoints;

/// <summary>
/// Creates or updates a transform step in a field mapping's transform chain.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SaveFieldMappingTransformationEndpoint : SaveFieldMappingTransformationEndpointBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="SaveFieldMappingTransformationEndpoint"/>.
    /// </summary>
    public SaveFieldMappingTransformationEndpoint(IConfigurationGatewayProvider configurationGateways, TransformationConfigurationProvider transformations, ILogger<SaveFieldMappingTransformationEndpoint> logger)
        : base(logger, configurationGateways, transformations) {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Transformations");
    }
}
