using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The LookupConnections endpoint.</summary>
[WebMcpTool("lookup_connections", "Look up connections by name.", ReadOnly = true)]
[TypeOption(typeof(ConnectionEndpoints), "LookupConnections")]
public class LookupConnectionsOption : ConnectionEndpointBase<LookupConnectionsEndpoint>
{
}
