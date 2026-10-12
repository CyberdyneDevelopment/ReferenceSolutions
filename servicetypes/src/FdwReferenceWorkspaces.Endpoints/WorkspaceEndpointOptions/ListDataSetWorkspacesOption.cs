using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The ListDataSetWorkspaces endpoint — GET /datasets/{DataSetName}/workspaces.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("list_data_set_workspaces", "List the data projects a data set belongs to that the caller has, with whether the caller may query each.", ReadOnly = true)]
[TypeOption(typeof(WorkspaceEndpoints), "ListDataSetWorkspaces")]
public class ListDataSetWorkspacesOption : WorkspaceEndpointBase<ListDataSetWorkspacesEndpoint>
{
}
