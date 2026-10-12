using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCalculations.Endpoints.CalculationEntityEndpointOptions;

/// <summary>The ExecuteCalculationEntity endpoint.</summary>
[WebMcpTool("execute_calculation_entity", "Execute calculation entity.")]
[TypeOption(typeof(CalculationEntityEndpoints), "ExecuteCalculationEntity")]
public class ExecuteCalculationEntityOption : CalculationEntityEndpointBase<ExecuteCalculationEntityEndpoint>
{
}
