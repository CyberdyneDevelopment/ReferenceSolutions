using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The CreateDataSetVariation endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("create_data_set_variation", "Create a data set a data project owns as a variation of a main data set.")]
[TypeOption(typeof(WorkspaceEndpoints), "CreateDataSetVariation")]
public class CreateDataSetVariationOption : WorkspaceEndpointBase<CreateDataSetVariationEndpoint>
{
}
