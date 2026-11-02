using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The UpdateRoslynWorkspaceConnection endpoint.</summary>
[WebMcpTool("update_roslyn_workspace_connection", "Update roslyn workspace connection.")]
[TypeOption(typeof(ConnectionEndpoints), "UpdateRoslynWorkspaceConnection")]
public class UpdateRoslynWorkspaceConnectionOption : ConnectionEndpointBase<UpdateConnectionEndpoint>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateRoslynWorkspaceConnectionOption"/> class.</summary>
    public UpdateRoslynWorkspaceConnectionOption() : base("UpdateRoslynWorkspaceConnection")
    {
    }
}
