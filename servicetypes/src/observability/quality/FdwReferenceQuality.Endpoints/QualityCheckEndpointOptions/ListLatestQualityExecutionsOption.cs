using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceQuality.Endpoints.QualityCheckEndpointOptions;

/// <summary>The ListLatestQualityExecutions endpoint.</summary>
[WebMcpTool("list_latest_quality_executions", "Latest recorded execution of each quality rule of a data set.", ReadOnly = true)]
[TypeOption(typeof(QualityCheckEndpoints), "ListLatestQualityExecutions")]
public class ListLatestQualityExecutionsOption : QualityCheckEndpointBase<ListLatestQualityExecutionsEndpoint>
{
}
