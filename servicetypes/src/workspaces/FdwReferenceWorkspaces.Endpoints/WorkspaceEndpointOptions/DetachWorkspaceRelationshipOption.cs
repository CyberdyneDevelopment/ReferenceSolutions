using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The DetachWorkspaceRelationship endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("remove_workspace_relationship", "Remove a declared relationship from a data project.")]
[TypeOption(typeof(WorkspaceEndpoints), "DetachWorkspaceRelationship")]
public class DetachWorkspaceRelationshipOption : WorkspaceEndpointBase<DetachWorkspaceRelationshipEndpoint>
{
}
