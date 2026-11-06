using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceThresholds.Endpoints.ThresholdEndpointOptions;

/// <summary>The GetEscalationTypes endpoint.</summary>
[WebMcpTool("get_escalation_types", "Get escalation types.", ReadOnly = true)]
[TypeOption(typeof(ThresholdEndpoints), "GetEscalationTypes")]
public class GetEscalationTypesOption : ThresholdEndpointBase<GetEscalationTypesEndpoint>
{
}
