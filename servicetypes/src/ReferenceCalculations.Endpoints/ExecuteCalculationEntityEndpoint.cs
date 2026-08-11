using System.Diagnostics.CodeAnalysis;
using Fdw.Calculations.Endpoints.CalculationEntities;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Data.Abstractions;

namespace ReferenceCalculations.Endpoints;

/// <summary>
/// Executes a calculation entity and returns the result.
/// </summary>
[ExcludeFromCodeCoverage]
public class ExecuteCalculationEntityEndpoint : ExecuteCalculationEntityEndpointBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="ExecuteCalculationEntityEndpoint"/>.
    /// </summary>
    public ExecuteCalculationEntityEndpoint(ICalculationEntityService service, IDataGateway dataGateway)
        : base(service, dataGateway) { }
}
