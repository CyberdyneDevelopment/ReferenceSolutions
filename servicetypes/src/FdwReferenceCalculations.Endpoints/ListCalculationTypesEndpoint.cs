using System.Diagnostics.CodeAnalysis;
using Fdw.Calculations.Endpoints;
using Fdw.Services.Calculations.Abstractions.CalculationSources;
using Microsoft.Extensions.Logging;

namespace FdwReferenceCalculations.Endpoints;

/// <summary>
/// Closure for the list calculation types endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListCalculationTypesEndpoint : ListCalculationTypesEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ListCalculationTypesEndpoint"/> class.</summary>
    public ListCalculationTypesEndpoint(ILogger<ListCalculationTypesEndpoint> logger, ICalculationCatalogProvider catalog)
        : base(logger, catalog)
    {
    }

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
