using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The CreateWorkspace endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("create_workspace", "Create a data project.")]
[TypeOption(typeof(WorkspaceEndpoints), "CreateWorkspace")]
public class CreateWorkspaceOption : WorkspaceEndpointBase<CreateWorkspaceEndpoint>
{
}
