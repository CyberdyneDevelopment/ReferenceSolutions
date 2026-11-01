using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The GetMapChildren endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("explode_map_node", "Get only the children of one map node: a data store's containers, or the containers a data set is stored in.", ReadOnly = true)]
[TypeOption(typeof(WorkspaceEndpoints), "GetMapChildren")]
public class GetMapChildrenOption : WorkspaceEndpointBase<GetMapChildrenEndpoint>
{
}
