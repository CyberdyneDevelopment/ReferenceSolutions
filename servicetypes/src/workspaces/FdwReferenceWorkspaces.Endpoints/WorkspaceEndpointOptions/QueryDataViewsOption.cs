using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The QueryDataViews endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("query_data_views", "Query data views by filter, order, page and field.", ReadOnly = true)]
[TypeOption(typeof(WorkspaceEndpoints), "QueryDataViews")]
public class QueryDataViewsOption : WorkspaceEndpointBase<QueryDataViewsEndpoint>
{
}
