using System.Diagnostics.CodeAnalysis;
using Fdw.Calculations.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceCalculations.Endpoints;

/// <summary>
/// Closure for the list period comparison types endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListPeriodComparisonTypesEndpoint : ListPeriodComparisonTypesEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ListPeriodComparisonTypesEndpoint"/> class.</summary>
    public ListPeriodComparisonTypesEndpoint(ILogger<ListPeriodComparisonTypesEndpoint> logger) : base(logger)
    {
    }

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
