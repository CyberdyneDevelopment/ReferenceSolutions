using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The DenyWorkspaceMembershipRequest endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("deny_workspace_membership_request", "Decline a pending request to join a data project.")]
[TypeOption(typeof(WorkspaceEndpoints), "DenyWorkspaceMembershipRequest")]
public class DenyWorkspaceMembershipRequestOption : WorkspaceEndpointBase<DenyWorkspaceMembershipRequestEndpoint>
{
}
