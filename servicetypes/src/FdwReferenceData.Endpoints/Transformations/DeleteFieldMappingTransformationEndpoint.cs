using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;
using Fdw.Services.Data.Transformations;

namespace FdwReferenceTransformations.Endpoints;

/// <summary>
/// Soft-deletes a transform step from a field mapping's transform chain.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteFieldMappingTransformationEndpoint : DeleteFieldMappingTransformationEndpointBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="DeleteFieldMappingTransformationEndpoint"/>.
    /// </summary>
    public DeleteFieldMappingTransformationEndpoint(IConfigurationGatewayProvider configurationGateways, TransformationConfigurationProvider transformations, ILogger<DeleteFieldMappingTransformationEndpoint> logger)
        : base(logger, configurationGateways, transformations) {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Transformations");
    }
}
