using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The GetConnectionHealth endpoint.</summary>
[WebMcpTool("get_connection_health", "Get connection health.", ReadOnly = true)]
[TypeOption(typeof(ConnectionEndpoints), "GetConnectionHealth")]
public class GetConnectionHealthOption : ConnectionEndpointBase<GetConnectionHealthEndpoint>
{
}
