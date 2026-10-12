using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAnalytics.Endpoints.AnalyticsEndpointOptions;

/// <summary>The GetAnalytics endpoint.</summary>
[WebMcpTool("get_analytics", "Get analytics.", ReadOnly = true)]
[TypeOption(typeof(AnalyticsEndpoints), "GetAnalytics")]
public class GetAnalyticsOption : AnalyticsEndpointBase<GetAnalyticsEndpoint>
{
}
