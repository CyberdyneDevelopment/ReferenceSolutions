using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints.WorkspaceEndpointOptions;

/// <summary>The ListChartTypes endpoint.</summary>
[ExcludeFromCodeCoverage]
[WebMcpTool("list_chart_types", "List the chart types a data view can be drawn as, with the slots each takes.", ReadOnly = true)]
[TypeOption(typeof(WorkspaceEndpoints), "ListChartTypes")]
public class ListChartTypesOption : WorkspaceEndpointBase<ListChartTypesEndpoint>
{
}
