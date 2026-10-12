using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The RemoveWorkspaceMember endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("remove_workspace_member", "Revoke a user's or role's membership of a data project.")]
[TypeOption(typeof(WorkspaceEndpoints), "RemoveWorkspaceMember")]
public class RemoveWorkspaceMemberOption : WorkspaceEndpointBase<RemoveWorkspaceMemberEndpoint>
{
}
