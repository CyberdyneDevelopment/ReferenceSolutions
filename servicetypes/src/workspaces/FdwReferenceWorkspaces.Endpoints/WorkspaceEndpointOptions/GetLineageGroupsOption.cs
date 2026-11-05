using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The GetLineageGroups endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("get_lineage_groups", "Group the data sets by category and the containers pipelines use by data store and path, and return the lines pipelines draw from one group to another, so a map can start above data-set level.", ReadOnly = true)]
[TypeOption(typeof(WorkspaceEndpoints), "GetLineageGroups")]
public class GetLineageGroupsOption : WorkspaceEndpointBase<GetLineageGroupsEndpoint>
{
}
