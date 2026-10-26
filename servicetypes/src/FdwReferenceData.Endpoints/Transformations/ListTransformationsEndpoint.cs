using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Endpoints;
using Fdw.Services.Data.Transformations;
using Fdw.Services.Settings;

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
    public ListTransformationsEndpoint(
        TransformationConfigurationProvider transformations,
        IEffectiveSettingsProvider settings,
        ILogger<ListTransformationsEndpoint> logger)
        : base(logger, transformations, settings) {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Transformations");
    }
}
