using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The GetDataSetFieldStandIn endpoint.</summary>
[WebMcpTool("get_data_set_field_stand_in", "Get a data set field's stand-in strategy.", ReadOnly = true)]
[TypeOption(typeof(DataSetEndpoints), "GetDataSetFieldStandIn")]
public class GetDataSetFieldStandInOption : DataSetEndpointBase<GetDataSetFieldStandInEndpoint>
{
}
