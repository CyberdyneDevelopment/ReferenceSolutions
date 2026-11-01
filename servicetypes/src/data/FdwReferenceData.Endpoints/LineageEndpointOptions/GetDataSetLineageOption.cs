using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceData.Endpoints.LineageEndpointOptions;

/// <summary>The GetDataSetLineage endpoint.</summary>
[WebMcpTool("get_data_set_lineage", "Get data set lineage.", ReadOnly = true)]
[TypeOption(typeof(DataLineageEndpoints), "GetDataSetLineage")]
public class GetDataSetLineageOption : DataLineageEndpointBase<GetDataSetLineageEndpoint>
{
}
