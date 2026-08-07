using System.Diagnostics.CodeAnalysis;
using Fdw.Calculations.Endpoints;

namespace Reference.Api.Endpoints.Calculations;

/// <summary>
/// Closure for the preview calculation endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class PreviewCalculationEndpoint : PreviewCalculationEndpointBase
{
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
