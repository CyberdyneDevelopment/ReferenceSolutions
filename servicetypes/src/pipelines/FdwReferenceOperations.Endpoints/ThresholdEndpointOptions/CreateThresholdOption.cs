using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceThresholds.Endpoints.ThresholdEndpointOptions;

/// <summary>The CreateThreshold endpoint.</summary>
[WebMcpTool("create_threshold", "Create threshold.")]
[TypeOption(typeof(ThresholdEndpoints), "CreateThreshold")]
public class CreateThresholdOption : ThresholdEndpointBase<CreateThresholdEndpoint>
{
}
