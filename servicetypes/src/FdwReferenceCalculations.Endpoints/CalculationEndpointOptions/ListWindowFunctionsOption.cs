using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCalculations.Endpoints.CalculationEndpointOptions;

/// <summary>The ListWindowFunctions endpoint.</summary>
[WebMcpTool("list_window_functions", "List the window functions a windowed calculation may name", ReadOnly = true)]
[TypeOption(typeof(CalculationEndpoints), "ListWindowFunctions")]
public class ListWindowFunctionsOption : CalculationEndpointBase<ListWindowFunctionsEndpoint>
{
}
