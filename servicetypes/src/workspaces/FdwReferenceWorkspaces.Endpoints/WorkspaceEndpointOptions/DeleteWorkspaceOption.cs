using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The DeleteWorkspace endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("delete_workspace", "Delete a data project.")]
[TypeOption(typeof(WorkspaceEndpoints), "DeleteWorkspace")]
public class DeleteWorkspaceOption : WorkspaceEndpointBase<DeleteWorkspaceEndpoint>
{
}
