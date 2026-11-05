using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceThresholds.Endpoints.ThresholdEndpointOptions;

/// <summary>The ListThresholds endpoint.</summary>
[WebMcpTool("list_thresholds", "List thresholds.", ReadOnly = true)]
[TypeOption(typeof(ThresholdEndpoints), "ListThresholds")]
public class ListThresholdsOption : ThresholdEndpointBase<ListThresholdsEndpoint>
{
}
