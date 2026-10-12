using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The GetMap endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("get_map", "Stream the map of data stores, pipelines and data sets for everything visible, one organization or one data project.", ReadOnly = true)]
[TypeOption(typeof(WorkspaceEndpoints), "GetMap")]
public class GetMapOption : WorkspaceEndpointBase<GetMapEndpoint>
{
}
