using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.DataSetSourceEndpointOptions;

/// <summary>The SearchDataSetSource endpoint.</summary>
[WebMcpTool("search_data_set_source", "Search for DataSets to bind as a source", ReadOnly = true)]
[TypeOption(typeof(DataSetSourceEndpoints), "SearchDataSetSource")]
public class SearchDataSetSourceOption : DataSetSourceEndpointBase<SearchDataSetSourceEndpoint>
{
}
