using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The QueryDataSet endpoint.</summary>
[WebMcpTool("query_data_set", "Query a DataSet")]
[TypeOption(typeof(DataSetEndpoints), "QueryDataSet")]
public class QueryDataSetOption : DataSetEndpointBase<QueryDataSetEndpoint>
{
}
