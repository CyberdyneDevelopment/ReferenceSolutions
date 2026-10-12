using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The ListWorkspaceNotes endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("list_workspace_notes", "List the notes raised in a data project, newest first.", ReadOnly = true)]
[TypeOption(typeof(WorkspaceEndpoints), "ListWorkspaceNotes")]
public class ListWorkspaceNotesOption : WorkspaceEndpointBase<ListWorkspaceNotesEndpoint>
{
}
