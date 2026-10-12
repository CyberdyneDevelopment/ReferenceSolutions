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
public sealed class SaveFieldMappingTransformEndpoint : SaveFieldMappingTransformEndpointBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="SaveFieldMappingTransformEndpoint"/>.
    /// </summary>
    public SaveFieldMappingTransformEndpoint(IConfigurationGatewayProvider configurationGateways, TransformationConfigurationProvider transformations, ILogger<SaveFieldMappingTransformEndpoint> logger)
        : base(logger, configurationGateways, transformations) {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Transformations");
    }
}
