using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceThresholds.Endpoints.ThresholdEndpointOptions;

/// <summary>The AttachThreshold endpoint.</summary>
[WebMcpTool("attach_threshold", "Attach threshold.")]
[TypeOption(typeof(ThresholdEndpoints), "AttachThreshold")]
public class AttachThresholdOption : ThresholdEndpointBase<AttachThresholdEndpoint>
{
}
