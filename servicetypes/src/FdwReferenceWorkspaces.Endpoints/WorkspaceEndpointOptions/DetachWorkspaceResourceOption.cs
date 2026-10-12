using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The DetachWorkspaceResource endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("detach_workspace_resource", "Remove a resource from a data project's map.")]
[TypeOption(typeof(WorkspaceEndpoints), "DetachWorkspaceResource")]
public class DetachWorkspaceResourceOption : WorkspaceEndpointBase<DetachWorkspaceResourceEndpoint>
{
}
