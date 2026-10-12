using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The GetConnection endpoint.</summary>
[WebMcpTool("get_connection", "Get connection.", ReadOnly = true)]
[TypeOption(typeof(ConnectionEndpoints), "GetConnection")]
public class GetConnectionOption : ConnectionEndpointBase<GetConnectionEndpoint>
{
}
