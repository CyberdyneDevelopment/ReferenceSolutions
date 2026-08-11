using System.Diagnostics.CodeAnalysis;
using Fdw.Calculations.Endpoints;

namespace ReferenceCalculations.Endpoints;

/// <summary>
/// Closure for the list period comparison types endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListPeriodComparisonTypesEndpoint : ListPeriodComparisonTypesEndpointBase
{
    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("datasets:read");
        Summary(s =>
        {
            s.Summary = "List available period comparison types";
            s.Description = "Returns all registered period comparison types for time-series analysis";
        });
        Tags("Calculations");
    }
}
