using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Endpoints;
using Fdw.Services.Data.Transformations;

namespace FdwReferenceTransformations.Endpoints;

/// <summary>
/// Lists every transformation, standard and custom.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListTransformationsEndpoint : ListTransformationsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="ListTransformationsEndpoint"/>.
    /// </summary>
    public ListTransformationsEndpoint(TransformationConfigurationProvider transformations, ILogger<ListTransformationsEndpoint> logger)
        : base(logger, transformations) {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Transformations");
    }
}
