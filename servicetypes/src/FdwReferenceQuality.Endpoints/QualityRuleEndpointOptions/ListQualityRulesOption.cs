using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceQuality.Endpoints.QualityRuleEndpointOptions;

/// <summary>The ListQualityRules endpoint.</summary>
[WebMcpTool("list_quality_rules", "List quality rules.", ReadOnly = true)]
[TypeOption(typeof(QualityRuleEndpoints), "ListQualityRules")]
public class ListQualityRulesOption : QualityRuleEndpointBase<ListQualityRulesEndpoint>
{
}
