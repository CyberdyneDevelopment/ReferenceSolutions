using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The GetDataSetFieldHistory endpoint.</summary>
/// <remarks>
/// Auto-discovery is off, so this declaration is what routes the endpoint. Without it the class
/// compiles, the host starts, and the field history route 404s.
/// </remarks>
[WebMcpTool("get_data_set_field_history", "Get every stored version of a data set field, oldest first, with what changed in each.", ReadOnly = true)]
[TypeOption(typeof(DataSetEndpoints), "GetDataSetFieldHistory")]
public class GetDataSetFieldHistoryOption : DataSetEndpointBase<GetDataSetFieldHistoryEndpoint>
{
}
