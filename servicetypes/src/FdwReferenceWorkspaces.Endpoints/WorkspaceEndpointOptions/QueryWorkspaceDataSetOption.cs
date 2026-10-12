using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The QueryWorkspaceDataSet endpoint — POST /{WorkspaceName}/{DataSetName}.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("query_workspace_data_set", "Query rows of one data set attached to a data project the caller may read.", ReadOnly = true)]
[TypeOption(typeof(WorkspaceEndpoints), "QueryWorkspaceDataSet")]
public class QueryWorkspaceDataSetOption : WorkspaceEndpointBase<QueryWorkspaceDataSetEndpoint>
{
}
