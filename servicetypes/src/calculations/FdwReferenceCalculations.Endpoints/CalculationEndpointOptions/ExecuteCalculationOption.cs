using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCalculations.Endpoints.CalculationEndpointOptions;

/// <summary>The ExecuteCalculation endpoint.</summary>
[WebMcpTool("execute_calculation", "Execute a calculation")]
[TypeOption(typeof(CalculationEndpoints), "ExecuteCalculation")]
public class ExecuteCalculationOption : CalculationEndpointBase<ExecuteCalculationEndpoint>
{
}
