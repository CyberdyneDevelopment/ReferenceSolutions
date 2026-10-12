using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The GetConnectionsByType endpoint.</summary>
[WebMcpTool("get_connections_by_type", "Get connections by type.", ReadOnly = true)]
[TypeOption(typeof(ConnectionEndpoints), "GetConnectionsByType")]
public class GetConnectionsByTypeOption : ConnectionEndpointBase<GetConnectionsByTypeEndpoint>
{
}
