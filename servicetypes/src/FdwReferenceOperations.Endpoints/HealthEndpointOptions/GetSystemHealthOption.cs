using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceHealth.Endpoints.HealthEndpointOptions;

/// <summary>The GetSystemHealth endpoint.</summary>
[WebMcpTool("get_system_health", "Get system health.", ReadOnly = true)]
[TypeOption(typeof(HealthEndpoints), "GetSystemHealth")]
public class GetSystemHealthOption : HealthEndpointBase<GetSystemHealthEndpoint>
{
}
