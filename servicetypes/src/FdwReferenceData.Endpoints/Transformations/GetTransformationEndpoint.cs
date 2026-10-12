using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Endpoints;
using Fdw.Services.Data.Transformations;
using Fdw.Services.Settings;

namespace FdwReferenceTransformations.Endpoints;

/// <summary>
/// Gets one transformation by name.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetTransformationEndpoint : GetTransformationEndpointBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="GetTransformationEndpoint"/>.
    /// </summary>
    public GetTransformationEndpoint(
        TransformationConfigurationProvider transformations,
        IEffectiveSettingsProvider settings,
        ILogger<GetTransformationEndpoint> logger)
        : base(logger, transformations, settings) {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Transformations");
    }
}
