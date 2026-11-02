using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The ListConnections endpoint.</summary>
[WebMcpTool("list_connections", "List connections.", ReadOnly = true)]
[TypeOption(typeof(ConnectionEndpoints), "ListConnections")]
public class ListConnectionsOption : ConnectionEndpointBase<ListConnectionsEndpoint>
{
}
