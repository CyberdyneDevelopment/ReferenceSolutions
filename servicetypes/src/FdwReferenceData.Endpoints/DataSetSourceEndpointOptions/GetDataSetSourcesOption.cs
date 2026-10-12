using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.DataSetSourceEndpointOptions;

/// <summary>The GetDataSetSources endpoint.</summary>
[WebMcpTool("get_data_set_sources", "Get data set sources.", ReadOnly = true)]
[TypeOption(typeof(DataSetSourceEndpoints), "GetDataSetSources")]
public class GetDataSetSourcesOption : DataSetSourceEndpointBase<GetDataSetSourcesEndpoint>
{
}
