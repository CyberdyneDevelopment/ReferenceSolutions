using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The AddWorkspaceMember endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("add_workspace_member", "Grant a user or role membership of a data project.")]
[TypeOption(typeof(WorkspaceEndpoints), "AddWorkspaceMember")]
public class AddWorkspaceMemberOption : WorkspaceEndpointBase<AddWorkspaceMemberEndpoint>
{
}
