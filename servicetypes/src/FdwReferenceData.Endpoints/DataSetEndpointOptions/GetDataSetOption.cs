using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The GetDataSet endpoint.</summary>
[WebMcpTool("get_data_set", "Get data set.", ReadOnly = true)]
[TypeOption(typeof(DataSetEndpoints), "GetDataSet")]
public class GetDataSetOption : DataSetEndpointBase<GetDataSetEndpoint>
{
}
