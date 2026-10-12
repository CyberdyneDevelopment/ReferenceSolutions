using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceData.Endpoints.LineageEndpointOptions;

/// <summary>The GetDataflowGraph endpoint.</summary>
[WebMcpTool("get_dataflow_graph", "Get dataflow graph.", ReadOnly = true)]
[TypeOption(typeof(DataLineageEndpoints), "GetDataflowGraph")]
public class GetDataflowGraphOption : DataLineageEndpointBase<GetDataflowGraphEndpoint>
{
}
