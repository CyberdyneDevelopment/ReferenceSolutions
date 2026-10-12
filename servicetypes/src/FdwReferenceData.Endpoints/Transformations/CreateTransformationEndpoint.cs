using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Endpoints;
using Fdw.Services.Data.Transformations;
using Fdw.Services.Settings;

namespace FdwReferenceTransformations.Endpoints;

/// <summary>
/// Defines a custom transformation.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateTransformationEndpoint : CreateTransformationEndpointBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="CreateTransformationEndpoint"/>.
    /// </summary>
    public CreateTransformationEndpoint(
        TransformationConfigurationProvider transformations,
        IEffectiveSettingsProvider settings,
        ILogger<CreateTransformationEndpoint> logger)
        : base(logger, transformations, settings) {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Transformations");
    }
}
