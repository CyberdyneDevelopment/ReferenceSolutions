using System.Diagnostics.CodeAnalysis;
using Fdw.Calculations.Endpoints;

namespace Reference.Api.Endpoints.Calculations;

/// <summary>
/// Closure for the list calculation types endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListCalculationTypesEndpoint : ListCalculationTypesEndpointBase
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
