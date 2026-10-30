using System.Diagnostics.CodeAnalysis;
using Fdw.Calculations.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceCalculations.Endpoints;

/// <summary>
/// Closure for the list window functions endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListWindowFunctionsEndpoint : ListWindowFunctionsEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ListWindowFunctionsEndpoint"/> class.</summary>
    public ListWindowFunctionsEndpoint(ILogger<ListWindowFunctionsEndpoint> logger)
        : base(logger)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("datasets:read");
        Summary(s =>
        {
            s.Summary = "List window functions";
            s.Description = "Returns the window functions a windowed calculation may name in its WindowFunction.";
        });
        Tags("Calculations");
    }
}
