using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The ListDataViews endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("list_data_views", "List every data view.", ReadOnly = true)]
[TypeOption(typeof(WorkspaceEndpoints), "ListDataViews")]
public class ListDataViewsOption : WorkspaceEndpointBase<ListDataViewsEndpoint>
{
}
