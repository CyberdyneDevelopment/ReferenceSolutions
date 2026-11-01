using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The GetLineageNeighbourhood endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("get_lineage_neighbourhood", "Walk from one data set, data store or pipeline toward what feeds it, what it feeds, or both, and return only that neighbourhood with the pipeline named on every edge that passes through one.", ReadOnly = true)]
[TypeOption(typeof(WorkspaceEndpoints), "GetLineageNeighbourhood")]
public class GetLineageNeighbourhoodOption : WorkspaceEndpointBase<GetLineageNeighbourhoodEndpoint>
{
}
