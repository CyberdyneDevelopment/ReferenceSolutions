using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The GetWorkspaceMap endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("get_workspace_map", "Get a data project's resources as graph nodes and their relationships as edges.", ReadOnly = true)]
[TypeOption(typeof(WorkspaceEndpoints), "GetWorkspaceMap")]
public class GetWorkspaceMapOption : WorkspaceEndpointBase<GetWorkspaceMapEndpoint>
{
}
