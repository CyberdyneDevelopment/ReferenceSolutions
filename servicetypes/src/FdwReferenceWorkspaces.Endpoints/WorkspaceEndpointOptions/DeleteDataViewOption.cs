using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The DeleteDataView endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("delete_data_view", "Delete a data view that no other view is built on.")]
[TypeOption(typeof(WorkspaceEndpoints), "DeleteDataView")]
public class DeleteDataViewOption : WorkspaceEndpointBase<DeleteDataViewEndpoint>
{
}
