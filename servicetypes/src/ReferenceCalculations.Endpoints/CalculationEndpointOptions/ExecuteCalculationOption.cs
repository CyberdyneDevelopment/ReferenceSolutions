using Fdw.Collections.Attributes;

namespace ReferenceCalculations.Endpoints.CalculationEndpointOptions;

/// <summary>The ExecuteCalculation endpoint.</summary>
[TypeOption(typeof(CalculationEndpoints), "ExecuteCalculation")]
public class ExecuteCalculationOption : CalculationEndpointBase<ExecuteCalculationEndpoint>
{
}
