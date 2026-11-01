using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCalculations.Endpoints.CalculationEndpointOptions;

/// <summary>The ListCalculationNodeTypes endpoint.</summary>
[WebMcpTool("list_calculation_node_types", "List the operations a formula node can name, with their operand counts, members and modes", ReadOnly = true)]
[TypeOption(typeof(CalculationEndpoints), "ListCalculationNodeTypes")]
public class ListCalculationNodeTypesOption : CalculationEndpointBase<ListCalculationNodeTypesEndpoint>
{
}
