using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceQuality.Endpoints.QualityCheckEndpointOptions;

/// <summary>The ListQualityRuleExecutions endpoint.</summary>
[WebMcpTool("list_quality_rule_executions", "List the recorded executions of a quality rule, newest first.", ReadOnly = true)]
[TypeOption(typeof(QualityCheckEndpoints), "ListQualityRuleExecutions")]
public class ListQualityRuleExecutionsOption : QualityCheckEndpointBase<ListQualityRuleExecutionsEndpoint>
{
}
