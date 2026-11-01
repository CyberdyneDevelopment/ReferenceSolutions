using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The SetDataSetFieldStandIn endpoint.</summary>
[WebMcpTool("set_data_set_field_stand_in", "Set a data set field's stand-in strategy.")]
[TypeOption(typeof(DataSetEndpoints), "SetDataSetFieldStandIn")]
public class SetDataSetFieldStandInOption : DataSetEndpointBase<SetDataSetFieldStandInEndpoint>
{
}
