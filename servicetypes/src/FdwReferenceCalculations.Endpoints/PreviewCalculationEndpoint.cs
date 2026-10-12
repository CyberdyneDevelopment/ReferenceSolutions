using System.Diagnostics.CodeAnalysis;
using Fdw.Calculations.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceCalculations.Endpoints;

/// <summary>
/// Closure for the preview calculation endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public class PreviewCalculationEndpoint : PreviewCalculationEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="PreviewCalculationEndpoint"/> class.</summary>
    public PreviewCalculationEndpoint(ILogger<PreviewCalculationEndpoint> logger) : base(logger)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        // Why: preview is read-shaped (no persistence). Viewer with datasets:read should be allowed.
        Policies("datasets:read");
        Summary(s =>
        {
            s.Summary = "Preview a calculation";
            s.Description = "Generates sample data and executes the calculation for preview purposes";
        });
        Tags("Calculations");
    }
}
