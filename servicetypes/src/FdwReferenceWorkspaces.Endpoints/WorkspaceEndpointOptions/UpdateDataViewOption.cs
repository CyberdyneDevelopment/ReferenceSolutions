using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The UpdateDataView endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("update_data_view", "Change a data view: its name, description, filters, encoding or chart type.")]
[TypeOption(typeof(WorkspaceEndpoints), "UpdateDataView")]
public class UpdateDataViewOption : WorkspaceEndpointBase<UpdateDataViewEndpoint>
{
}
