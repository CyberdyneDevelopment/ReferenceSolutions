using System.Diagnostics.CodeAnalysis;
using Fdw.Calculations.Endpoints;

namespace ReferenceCalculations.Endpoints;

/// <summary>
/// Closure for the list calculation types endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListCalculationTypesEndpoint : ListCalculationTypesEndpointBase
{
    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("datasets:read");
        Summary(s =>
        {
            s.Summary = "List available calculation types";
            s.Description = "Returns all registered calculation types (Sum, Average, Count, Min, Max)";
        });
        Tags("Calculations");
    }
}
