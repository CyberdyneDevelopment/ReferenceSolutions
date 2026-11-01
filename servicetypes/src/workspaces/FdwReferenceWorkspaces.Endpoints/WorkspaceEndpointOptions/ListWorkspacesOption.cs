using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The ListWorkspaces endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("list_workspaces", "List the data projects visible to you.", ReadOnly = true)]
[TypeOption(typeof(WorkspaceEndpoints), "ListWorkspaces")]
public class ListWorkspacesOption : WorkspaceEndpointBase<ListWorkspacesEndpoint>
{
}
