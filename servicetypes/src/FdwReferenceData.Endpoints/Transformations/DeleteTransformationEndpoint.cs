using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;
using Fdw.Services.Data.Transformations;

namespace FdwReferenceTransformations.Endpoints;

/// <summary>
/// Deletes a custom transformation that is not attached to any field mapping.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteTransformationEndpoint : DeleteTransformationEndpointBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="DeleteTransformationEndpoint"/>.
    /// </summary>
    public DeleteTransformationEndpoint(IConfigurationGatewayProvider configurationGateways, TransformationConfigurationProvider transformations, ILogger<DeleteTransformationEndpoint> logger)
        : base(logger, configurationGateways, transformations) {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Transformations");
    }
}
