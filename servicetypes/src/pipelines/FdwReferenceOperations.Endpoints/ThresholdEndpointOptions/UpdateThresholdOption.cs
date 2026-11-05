using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceThresholds.Endpoints.ThresholdEndpointOptions;

/// <summary>The UpdateThreshold endpoint.</summary>
[WebMcpTool("update_threshold", "Replace threshold.")]
[TypeOption(typeof(ThresholdEndpoints), "UpdateThreshold")]
public class UpdateThresholdOption : ThresholdEndpointBase<UpdateThresholdEndpoint>
{
}
