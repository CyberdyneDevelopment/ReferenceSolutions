using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCalculations.Endpoints.CalculationEntityEndpointOptions;

/// <summary>The ListCalculationEntities endpoint.</summary>
[WebMcpTool("list_calculation_entities", "List calculation entities.", ReadOnly = true)]
[TypeOption(typeof(CalculationEntityEndpoints), "ListCalculationEntities")]
public class ListCalculationEntitiesOption : CalculationEntityEndpointBase<ListCalculationEntitiesEndpoint>
{
}
