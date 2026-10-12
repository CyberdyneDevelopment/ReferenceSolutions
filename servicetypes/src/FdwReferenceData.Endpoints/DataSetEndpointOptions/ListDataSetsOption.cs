using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The ListDataSets endpoint.</summary>
[WebMcpTool("list_data_sets", "List data sets.", ReadOnly = true)]
[TypeOption(typeof(DataSetEndpoints), "ListDataSets")]
public class ListDataSetsOption : DataSetEndpointBase<ListDataSetsEndpoint>
{
}
