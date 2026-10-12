using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceData.Endpoints.LineageEndpointOptions;

/// <summary>The GetImpactAnalysis endpoint.</summary>
[WebMcpTool("get_impact_analysis", "Get impact analysis.", ReadOnly = true)]
[TypeOption(typeof(DataLineageEndpoints), "GetImpactAnalysis")]
public class GetImpactAnalysisOption : DataLineageEndpointBase<GetImpactAnalysisEndpoint>
{
}
