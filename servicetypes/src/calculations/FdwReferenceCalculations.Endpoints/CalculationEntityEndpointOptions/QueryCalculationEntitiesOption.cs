using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceCalculations.Endpoints.CalculationEntityEndpointOptions;

/// <summary>The QueryCalculationEntities endpoint.</summary>
[WebMcpTool("query_calculation_entities", "Query calculation entities by filter, order, page and field.", ReadOnly = true)]
[TypeOption(typeof(CalculationEntityEndpoints), "QueryCalculationEntities")]
public class QueryCalculationEntitiesOption : CalculationEntityEndpointBase<QueryCalculationEntitiesEndpoint>
{
}
