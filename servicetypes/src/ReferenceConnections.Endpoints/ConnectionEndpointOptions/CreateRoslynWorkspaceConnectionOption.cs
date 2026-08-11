using Fdw.Collections.Attributes;

namespace ReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>The CreateRoslynWorkspaceConnection endpoint.</summary>
[TypeOption(typeof(ConnectionEndpoints), "CreateRoslynWorkspaceConnection")]
public class CreateRoslynWorkspaceConnectionOption : ConnectionEndpointBase<CreateRoslynWorkspaceConnectionEndpoint>
{
}
