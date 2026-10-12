using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The GetMapSummary endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("get_map_summary", "Count the data sets, data stores and pipelines on the map for everything visible, one organization or one data project, without reading them.", ReadOnly = true)]
[TypeOption(typeof(WorkspaceEndpoints), "GetMapSummary")]
public class GetMapSummaryOption : WorkspaceEndpointBase<GetMapSummaryEndpoint>
{
}
