using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The ApproveWorkspaceMembershipRequest endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("approve_workspace_membership_request", "Approve a pending request to join a data project, granting membership.")]
[TypeOption(typeof(WorkspaceEndpoints), "ApproveWorkspaceMembershipRequest")]
public class ApproveWorkspaceMembershipRequestOption : WorkspaceEndpointBase<ApproveWorkspaceMembershipRequestEndpoint>
{
}
