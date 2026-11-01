using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The GetWorkspaceReadiness endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("get_workspace_readiness", "Count a data project's data sets by how much of each is bound to real data.", ReadOnly = true)]
[TypeOption(typeof(WorkspaceEndpoints), "GetWorkspaceReadiness")]
public class GetWorkspaceReadinessOption : WorkspaceEndpointBase<GetWorkspaceReadinessEndpoint>
{
}
