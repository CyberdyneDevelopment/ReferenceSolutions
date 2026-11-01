using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The CreateWorkspaceRelationship endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("create_workspace_relationship", "Declare a relationship between two data sets in a data project.")]
[TypeOption(typeof(WorkspaceEndpoints), "CreateWorkspaceRelationship")]
public class CreateWorkspaceRelationshipOption : WorkspaceEndpointBase<CreateWorkspaceRelationshipEndpoint>
{
}
