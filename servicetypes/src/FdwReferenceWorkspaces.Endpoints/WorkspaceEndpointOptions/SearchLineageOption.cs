using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The SearchLineage endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("search_lineage", "Find data sets, data stores and pipelines by name, to pick the node a lineage view is centred on.", ReadOnly = true)]
[TypeOption(typeof(WorkspaceEndpoints), "SearchLineage")]
public class SearchLineageOption : WorkspaceEndpointBase<SearchLineageEndpoint>
{
}
