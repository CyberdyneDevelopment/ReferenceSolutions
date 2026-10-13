using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The DeleteDataSetVariation endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("delete_data_set_variation", "Delete a data set a data project owns as a variation of a main data set.")]
[TypeOption(typeof(WorkspaceEndpoints), "DeleteDataSetVariation")]
public class DeleteDataSetVariationOption : WorkspaceEndpointBase<DeleteDataSetVariationEndpoint>
{
}
