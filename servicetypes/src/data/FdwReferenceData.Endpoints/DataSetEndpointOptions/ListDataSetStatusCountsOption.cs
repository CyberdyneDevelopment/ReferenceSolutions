using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The ListDataSetStatusCounts endpoint.</summary>
[WebMcpTool("list_data_set_status_counts", "Count the data sets by status.", ReadOnly = true)]
[TypeOption(typeof(DataSetEndpoints), "ListDataSetStatusCounts")]
public class ListDataSetStatusCountsOption : DataSetEndpointBase<ListDataSetStatusCountsEndpoint>
{
}
