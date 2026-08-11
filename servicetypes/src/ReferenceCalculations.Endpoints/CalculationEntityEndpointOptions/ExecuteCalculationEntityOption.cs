using Fdw.Collections.Attributes;

namespace ReferenceCalculations.Endpoints.CalculationEntityEndpointOptions;

/// <summary>The ExecuteCalculationEntity endpoint.</summary>
[TypeOption(typeof(CalculationEntityEndpoints), "ExecuteCalculationEntity")]
public class ExecuteCalculationEntityOption : CalculationEntityEndpointBase<ExecuteCalculationEntityEndpoint>
{
}
