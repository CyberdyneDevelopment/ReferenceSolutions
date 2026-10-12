using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceEtl.Endpoints.Lineage.LineageEndpointOptions;

/// <summary>The GetLineageGraph endpoint.</summary>
[WebMcpTool("get_lineage_graph", "Get lineage graph.", ReadOnly = true)]
[TypeOption(typeof(LineageEndpoints), "GetLineageGraph")]
public class GetLineageGraphOption : LineageEndpointBase<GetLineageGraphEndpoint>
{
}
