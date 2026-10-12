using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The ListWorkspaceMembers endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("list_workspace_members", "List everyone and every role holding membership in a data project.", ReadOnly = true)]
[TypeOption(typeof(WorkspaceEndpoints), "ListWorkspaceMembers")]
public class ListWorkspaceMembersOption : WorkspaceEndpointBase<ListWorkspaceMembersEndpoint>
{
}
