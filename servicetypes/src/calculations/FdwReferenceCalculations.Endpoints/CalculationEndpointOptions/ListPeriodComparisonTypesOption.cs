using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCalculations.Endpoints.CalculationEndpointOptions;

/// <summary>The ListPeriodComparisonTypes endpoint.</summary>
[WebMcpTool("list_period_comparison_types", "List available period comparison types", ReadOnly = true)]
[TypeOption(typeof(CalculationEndpoints), "ListPeriodComparisonTypes")]
public class ListPeriodComparisonTypesOption : CalculationEndpointBase<ListPeriodComparisonTypesEndpoint>
{
}
