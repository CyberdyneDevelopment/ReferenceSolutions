using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceEtl.Endpoints.Lineage.LineageEndpointOptions;

/// <summary>The ExpandLineageNode endpoint.</summary>
[WebMcpTool("expand_lineage_node", "Expand lineage node.")]
[TypeOption(typeof(LineageEndpoints), "ExpandLineageNode")]
public class ExpandOperationLineageNodeOption : LineageEndpointBase<ExpandOperationLineageNodeEndpoint>
{
}
