using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceThresholds.Endpoints.ThresholdEndpointOptions;

/// <summary>The GetThreshold endpoint.</summary>
[WebMcpTool("get_threshold", "Get threshold.", ReadOnly = true)]
[TypeOption(typeof(ThresholdEndpoints), "GetThreshold")]
public class GetThresholdOption : ThresholdEndpointBase<GetThresholdEndpoint>
{
}
