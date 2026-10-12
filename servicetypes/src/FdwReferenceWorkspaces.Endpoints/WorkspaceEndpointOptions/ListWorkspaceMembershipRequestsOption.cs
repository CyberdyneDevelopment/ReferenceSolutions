using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The ListWorkspaceMembershipRequests endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("list_workspace_membership_requests", "List every request to join a data project, reviewed or not.", ReadOnly = true)]
[TypeOption(typeof(WorkspaceEndpoints), "ListWorkspaceMembershipRequests")]
public class ListWorkspaceMembershipRequestsOption : WorkspaceEndpointBase<ListWorkspaceMembershipRequestsEndpoint>
{
}
