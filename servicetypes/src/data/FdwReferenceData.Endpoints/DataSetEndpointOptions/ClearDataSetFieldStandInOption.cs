using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceDataSets.Endpoints.DataSetEndpointOptions;

/// <summary>The ClearDataSetFieldStandIn endpoint.</summary>
[WebMcpTool("clear_data_set_field_stand_in", "Clear a data set field's stand-in strategy.")]
[TypeOption(typeof(DataSetEndpoints), "ClearDataSetFieldStandIn")]
public class ClearDataSetFieldStandInOption : DataSetEndpointBase<ClearDataSetFieldStandInEndpoint>
{
}
