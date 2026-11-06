using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceThresholds.Endpoints.ThresholdEndpointOptions;

/// <summary>The DeleteThreshold endpoint.</summary>
[WebMcpTool("delete_threshold", "Delete threshold.")]
[TypeOption(typeof(ThresholdEndpoints), "DeleteThreshold")]
public class DeleteThresholdOption : ThresholdEndpointBase<DeleteThresholdEndpoint>
{
}
