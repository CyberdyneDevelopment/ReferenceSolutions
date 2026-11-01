using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The ListStandInStrategies endpoint.</summary>
[WebMcpTool("list_stand_in_strategies", "List stand-in strategies.", ReadOnly = true)]
[TypeOption(typeof(DataSetEndpoints), "ListStandInStrategies")]
public class ListStandInStrategiesOption : DataSetEndpointBase<ListStandInStrategiesEndpoint>
{
}
