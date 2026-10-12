using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCalculations.Endpoints.CalculationEndpointOptions;

/// <summary>The ListCalculationTypes endpoint.</summary>
[WebMcpTool("list_calculation_types", "List available calculation types", ReadOnly = true)]
[TypeOption(typeof(CalculationEndpoints), "ListCalculationTypes")]
public class ListCalculationTypesOption : CalculationEndpointBase<ListCalculationTypesEndpoint>
{
}
