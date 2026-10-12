using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceEtl.Endpoints.Lineage.LineageEndpointOptions;

/// <summary>The GetLineageImpact endpoint.</summary>
[WebMcpTool("get_lineage_impact", "Get lineage impact.", ReadOnly = true)]
[TypeOption(typeof(LineageEndpoints), "GetLineageImpact")]
public class GetLineageImpactOption : LineageEndpointBase<GetLineageImpactEndpoint>
{
}
