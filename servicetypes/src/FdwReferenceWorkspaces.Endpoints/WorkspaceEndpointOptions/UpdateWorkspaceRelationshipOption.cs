using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The UpdateWorkspaceRelationship endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("update_workspace_relationship", "Set or change the join fields on a declared relationship between two data sets.")]
[TypeOption(typeof(WorkspaceEndpoints), "UpdateWorkspaceRelationship")]
public class UpdateWorkspaceRelationshipOption : WorkspaceEndpointBase<UpdateWorkspaceRelationshipEndpoint>
{
}
