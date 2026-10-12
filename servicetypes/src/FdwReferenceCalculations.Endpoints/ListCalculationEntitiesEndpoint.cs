using System.Diagnostics.CodeAnalysis;
using Fdw.Calculations.Endpoints.CalculationEntities;
using Fdw.Services.Calculations;
using Microsoft.Extensions.Logging;

namespace FdwReferenceCalculations.Endpoints;

/// <summary>
/// Lists calculation entities, one page at a time.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListCalculationEntitiesEndpoint : ListCalculationEntitiesEndpointBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="ListCalculationEntitiesEndpoint"/>.
    /// </summary>
    public ListCalculationEntitiesEndpoint(ILogger<ListCalculationEntitiesEndpoint> logger, ICalculationConfigurationProvider provider)
        : base(logger, provider)
    {
    }
}
