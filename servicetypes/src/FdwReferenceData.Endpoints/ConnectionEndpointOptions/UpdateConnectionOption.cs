using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The UpdateConnection endpoint.</summary>
[WebMcpTool("update_connection", "Update connection.")]
[TypeOption(typeof(ConnectionEndpoints), "UpdateConnection")]
public class UpdateConnectionOption : ConnectionEndpointBase<UpdateConnectionEndpoint>
{
}
