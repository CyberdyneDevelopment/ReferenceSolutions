using System.Diagnostics.CodeAnalysis;
using Fdw.Calculations.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceCalculations.Endpoints;

/// <summary>
/// Endpoint to list the operations a formula node can name, with their arity, members and modes (FDW-1280).
/// Sealed closure of the generic base class from Fdw.Calculations.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListCalculationNodeTypesEndpoint : ListCalculationNodeTypesEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ListCalculationNodeTypesEndpoint"/> class.</summary>
    public ListCalculationNodeTypesEndpoint(ILogger<ListCalculationNodeTypesEndpoint> logger) : base(logger)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Calculations");
    }
}
