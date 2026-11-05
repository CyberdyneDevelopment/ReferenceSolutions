using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceThresholds.Endpoints.ThresholdEndpointOptions;

/// <summary>The GetThresholdTypes endpoint.</summary>
[WebMcpTool("get_threshold_types", "Get threshold types.", ReadOnly = true)]
[TypeOption(typeof(ThresholdEndpoints), "GetThresholdTypes")]
public class GetThresholdTypesOption : ThresholdEndpointBase<GetThresholdTypesEndpoint>
{
}
