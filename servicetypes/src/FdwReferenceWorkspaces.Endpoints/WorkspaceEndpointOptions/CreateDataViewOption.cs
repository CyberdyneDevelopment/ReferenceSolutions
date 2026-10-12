using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The CreateDataView endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("create_data_view", "Save a stored visualisation of a data set.")]
[TypeOption(typeof(WorkspaceEndpoints), "CreateDataView")]
public class CreateDataViewOption : WorkspaceEndpointBase<CreateDataViewEndpoint>
{
}
