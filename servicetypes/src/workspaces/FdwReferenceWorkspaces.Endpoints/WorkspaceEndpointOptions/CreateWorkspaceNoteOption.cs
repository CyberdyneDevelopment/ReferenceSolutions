using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The CreateWorkspaceNote endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("create_workspace_note", "Write a note in a data project against a subject.")]
[TypeOption(typeof(WorkspaceEndpoints), "CreateWorkspaceNote")]
public class CreateWorkspaceNoteOption : WorkspaceEndpointBase<CreateWorkspaceNoteEndpoint>
{
}
