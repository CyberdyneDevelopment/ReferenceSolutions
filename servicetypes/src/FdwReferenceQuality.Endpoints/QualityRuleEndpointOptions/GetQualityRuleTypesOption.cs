using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceQuality.Endpoints.QualityRuleEndpointOptions;

/// <summary>The GetQualityRuleTypes endpoint.</summary>
[WebMcpTool("get_quality_rule_types", "List the quality rule types and severities this host accepts.", ReadOnly = true)]
[TypeOption(typeof(QualityRuleEndpoints), "GetQualityRuleTypes")]
public class GetQualityRuleTypesOption : QualityRuleEndpointBase<GetQualityRuleTypesEndpoint>
{
}
