using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The GetDataView endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("get_data_view", "Get one data view, with its filters.", ReadOnly = true)]
[TypeOption(typeof(WorkspaceEndpoints), "GetDataView")]
public class GetDataViewOption : WorkspaceEndpointBase<GetDataViewEndpoint>
{
}
