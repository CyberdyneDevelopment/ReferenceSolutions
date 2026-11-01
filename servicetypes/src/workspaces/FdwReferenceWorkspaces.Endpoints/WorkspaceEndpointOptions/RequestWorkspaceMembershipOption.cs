using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The RequestWorkspaceMembership endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("request_workspace_membership", "Ask to join a data project, honouring its join policy.")]
[TypeOption(typeof(WorkspaceEndpoints), "RequestWorkspaceMembership")]
public class RequestWorkspaceMembershipOption : WorkspaceEndpointBase<RequestWorkspaceMembershipEndpoint>
{
}
