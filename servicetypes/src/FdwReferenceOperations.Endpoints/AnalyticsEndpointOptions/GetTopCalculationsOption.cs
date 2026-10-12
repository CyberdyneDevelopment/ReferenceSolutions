using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAnalytics.Endpoints.AnalyticsEndpointOptions;

/// <summary>The GetTopCalculations endpoint.</summary>
[WebMcpTool("get_top_calculations", "Get top calculations.", ReadOnly = true)]
[TypeOption(typeof(AnalyticsEndpoints), "GetTopCalculations")]
public class GetTopCalculationsOption : AnalyticsEndpointBase<GetTopCalculationsEndpoint>
{
}
