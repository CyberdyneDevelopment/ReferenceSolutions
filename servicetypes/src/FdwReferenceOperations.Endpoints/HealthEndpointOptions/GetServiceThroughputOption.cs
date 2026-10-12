using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceHealth.Endpoints.HealthEndpointOptions;

/// <summary>The GetServiceThroughput endpoint.</summary>
[WebMcpTool("get_service_throughput", "Get service throughput.", ReadOnly = true)]
[TypeOption(typeof(HealthEndpoints), "GetServiceThroughput")]
public class GetServiceThroughputOption : HealthEndpointBase<GetServiceThroughputEndpoint>
{
}
