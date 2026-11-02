using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The GetLineagePath endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("get_lineage_path", "Find the fewest steps data takes from one data set or data store to another.", ReadOnly = true)]
[TypeOption(typeof(WorkspaceEndpoints), "GetLineagePath")]
public class GetLineagePathOption : WorkspaceEndpointBase<GetLineagePathEndpoint>
{
}
