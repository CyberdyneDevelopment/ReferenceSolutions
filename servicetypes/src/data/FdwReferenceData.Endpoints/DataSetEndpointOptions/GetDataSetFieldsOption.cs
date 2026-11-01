using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The GetDataSetFields endpoint.</summary>
[WebMcpTool("get_data_set_fields", "Get data set fields.", ReadOnly = true)]
[TypeOption(typeof(DataSetEndpoints), "GetDataSetFields")]
public class GetDataSetFieldsOption : DataSetEndpointBase<GetDataSetFieldsEndpoint>
{
}
