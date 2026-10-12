using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The UpdateHttpConnection endpoint.</summary>
[WebMcpTool("update_http_connection", "Update HTTP connection.")]
[TypeOption(typeof(ConnectionEndpoints), "UpdateHttpConnection")]
public class UpdateHttpConnectionOption : ConnectionEndpointBase<UpdateConnectionEndpoint>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateHttpConnectionOption"/> class.</summary>
    public UpdateHttpConnectionOption() : base("UpdateHttpConnection")
    {
    }
}
