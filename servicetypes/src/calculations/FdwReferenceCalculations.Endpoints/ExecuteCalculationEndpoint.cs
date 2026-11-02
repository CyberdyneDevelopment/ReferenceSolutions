using System.Diagnostics.CodeAnalysis;
using Fdw.Calculations.Endpoints;
using Fdw.Data.DataSets.Abstractions;
using Microsoft.Extensions.Logging;

namespace FdwReferenceCalculations.Endpoints;

/// <summary>
/// Closure for the execute calculation endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public class ExecuteCalculationEndpoint : ExecuteCalculationEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ExecuteCalculationEndpoint"/> class.</summary>
    public ExecuteCalculationEndpoint(
        ILogger<ExecuteCalculationEndpoint> logger,
        IDataSetResolver resolver)
        : base(logger, resolver)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("datasets:write");
        Summary(s =>
        {
            s.Summary = "Execute a calculation";
            s.Description = "Executes the specified calculation type on the provided array of values.";
        });
        Tags("Calculations");
    }
}
