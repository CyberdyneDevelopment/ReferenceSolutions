using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceThresholds.Endpoints.ThresholdEndpointOptions;

/// <summary>The GetSubjectThresholds endpoint.</summary>
[WebMcpTool("get_subject_thresholds", "Get subject thresholds.", ReadOnly = true)]
[TypeOption(typeof(ThresholdEndpoints), "GetSubjectThresholds")]
public class GetSubjectThresholdsOption : ThresholdEndpointBase<GetSubjectThresholdsEndpoint>
{
}
