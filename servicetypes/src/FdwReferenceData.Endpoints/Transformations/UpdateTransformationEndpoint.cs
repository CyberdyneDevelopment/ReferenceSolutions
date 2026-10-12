using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Endpoints;
using Fdw.Services.Data.Transformations;
using Fdw.Services.Settings;

namespace FdwReferenceTransformations.Endpoints;

/// <summary>
/// Changes a custom transformation; every field mapping it is attached to sees the change.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateTransformationEndpoint : UpdateTransformationEndpointBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="UpdateTransformationEndpoint"/>.
    /// </summary>
    public UpdateTransformationEndpoint(
        TransformationConfigurationProvider transformations,
        IEffectiveSettingsProvider settings,
        ILogger<UpdateTransformationEndpoint> logger)
        : base(logger, transformations, settings) {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Transformations");
    }
}
