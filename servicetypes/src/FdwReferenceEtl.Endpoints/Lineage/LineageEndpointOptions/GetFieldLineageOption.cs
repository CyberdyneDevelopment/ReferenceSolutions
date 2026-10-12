using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceEtl.Endpoints.Lineage.LineageEndpointOptions;

/// <summary>The GetFieldLineage endpoint.</summary>
[WebMcpTool("get_field_lineage", "Get field lineage.", ReadOnly = true)]
[TypeOption(typeof(LineageEndpoints), "GetFieldLineage")]
public class GetFieldLineageOption : LineageEndpointBase<GetFieldLineageEndpoint>
{
}
