using System.Diagnostics.CodeAnalysis;
using Fdw.Calculations.Endpoints;

namespace Reference.Api.Endpoints.Calculations;

/// <summary>
/// Closure for the list period comparison types endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListPeriodComparisonTypesEndpoint : ListPeriodComparisonTypesEndpointBase
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
