using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The GetWorkspace endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("get_workspace", "Get one data project with its members, resources and relationships.", ReadOnly = true)]
[TypeOption(typeof(WorkspaceEndpoints), "GetWorkspace")]
public class GetWorkspaceOption : WorkspaceEndpointBase<GetWorkspaceEndpoint>
{
}
