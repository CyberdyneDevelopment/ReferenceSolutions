using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The ListConnectionHealth endpoint.</summary>
[WebMcpTool("list_connection_health", "List current health status for every connection.", ReadOnly = true)]
[TypeOption(typeof(ConnectionEndpoints), "ListConnectionHealth")]
public class ListConnectionHealthOption : ConnectionEndpointBase<ListConnectionHealthEndpoint>
{
}
