using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceHealth.Endpoints.HealthEndpointOptions;

/// <summary>The GetServiceHealth endpoint.</summary>
[WebMcpTool("get_service_health", "Get service health.", ReadOnly = true)]
[TypeOption(typeof(HealthEndpoints), "GetServiceHealth")]
public class GetServiceHealthOption : HealthEndpointBase<GetServiceHealthEndpoint>
{
}
