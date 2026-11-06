using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceThresholds.Endpoints.ThresholdEndpointOptions;

/// <summary>The DetachThreshold endpoint.</summary>
[WebMcpTool("detach_threshold", "Detach threshold.")]
[TypeOption(typeof(ThresholdEndpoints), "DetachThreshold")]
public class DetachThresholdOption : ThresholdEndpointBase<DetachThresholdEndpoint>
{
}
