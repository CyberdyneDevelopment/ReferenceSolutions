using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceHealth.Endpoints.HealthEndpointOptions;

/// <summary>The GetServiceHealthHistory endpoint.</summary>
[WebMcpTool("get_service_health_history", "Get service health history.", ReadOnly = true)]
[TypeOption(typeof(HealthEndpoints), "GetServiceHealthHistory")]
public class GetServiceHealthHistoryOption : HealthEndpointBase<GetServiceHealthHistoryEndpoint>
{
}
