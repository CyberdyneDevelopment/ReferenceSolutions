using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The UpdateWorkspaceMemberRole endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("update_workspace_member_role", "Change the role a member holds on a data project.")]
[TypeOption(typeof(WorkspaceEndpoints), "UpdateWorkspaceMemberRole")]
public class UpdateWorkspaceMemberRoleOption : WorkspaceEndpointBase<UpdateWorkspaceMemberRoleEndpoint>
{
}
