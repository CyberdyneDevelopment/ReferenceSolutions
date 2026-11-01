using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The AttachWorkspaceResource endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("attach_workspace_resource", "Attach a data set or other resource to a data project's map.")]
[TypeOption(typeof(WorkspaceEndpoints), "AttachWorkspaceResource")]
public class AttachWorkspaceResourceOption : WorkspaceEndpointBase<AttachWorkspaceResourceEndpoint>
{
}
