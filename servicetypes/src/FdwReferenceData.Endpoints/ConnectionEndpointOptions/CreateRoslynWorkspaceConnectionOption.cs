using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Connections.RoslynWorkspace.Registration;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The CreateRoslynWorkspaceConnection endpoint.</summary>
[WebMcpTool(
    "create_roslyn_workspace_connection",
    "Create roslyn workspace connection.",
    PresetFieldName = nameof(CreateConnectionRequest.ServiceType),
    PresetOptionType = typeof(RoslynWorkspaceConnectionType))]
[TypeOption(typeof(ConnectionEndpoints), "CreateRoslynWorkspaceConnection")]
public class CreateRoslynWorkspaceConnectionOption : ConnectionEndpointBase<CreateConnectionEndpoint>
{
    /// <summary>Initializes a new instance of the <see cref="CreateRoslynWorkspaceConnectionOption"/> class.</summary>
    public CreateRoslynWorkspaceConnectionOption() : base("CreateRoslynWorkspaceConnection")
    {
    }
}
