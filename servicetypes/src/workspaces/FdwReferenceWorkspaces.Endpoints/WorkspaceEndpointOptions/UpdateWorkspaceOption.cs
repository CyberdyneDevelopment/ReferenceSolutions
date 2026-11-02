using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The UpdateWorkspace endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("update_workspace", "Modify a data project.")]
[TypeOption(typeof(WorkspaceEndpoints), "UpdateWorkspace")]
public class UpdateWorkspaceOption : WorkspaceEndpointBase<UpdateWorkspaceEndpoint>
{
}
