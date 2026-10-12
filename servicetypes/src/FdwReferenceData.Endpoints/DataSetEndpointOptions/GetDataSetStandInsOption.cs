using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The GetDataSetStandIns endpoint.</summary>
[WebMcpTool("get_data_set_stand_ins", "Get the stand-in strategy of every field of a data set.", ReadOnly = true)]
[TypeOption(typeof(DataSetEndpoints), "GetDataSetStandIns")]
public class GetDataSetStandInsOption : DataSetEndpointBase<GetDataSetStandInsEndpoint>
{
}
