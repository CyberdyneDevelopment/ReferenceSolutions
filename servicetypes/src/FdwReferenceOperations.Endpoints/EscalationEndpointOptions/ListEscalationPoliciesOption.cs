using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceEscalation.Endpoints.EscalationEndpointOptions;

/// <summary>The ListEscalationPolicies endpoint.</summary>
[WebMcpTool("list_escalation_policies", "List escalation policies.", ReadOnly = true)]
[TypeOption(typeof(EscalationEndpoints), "ListEscalationPolicies")]
public class ListEscalationPoliciesOption : EscalationEndpointBase<ListEscalationPoliciesEndpoint>
{
}
