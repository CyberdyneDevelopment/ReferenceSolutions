using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The DeleteConnection endpoint.</summary>
[WebMcpTool("delete_connection", "Delete connection.")]
[TypeOption(typeof(ConnectionEndpoints), "DeleteConnection")]
public class DeleteConnectionOption : ConnectionEndpointBase<DeleteConnectionEndpoint>
{
}
