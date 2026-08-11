using Fdw.Collections.Attributes;

namespace ReferenceQuality.Endpoints.QualityRuleEndpointOptions;

/// <summary>The ListQualityRules endpoint.</summary>
[TypeOption(typeof(QualityRuleEndpoints), "ListQualityRules")]
public class ListQualityRulesOption : QualityRuleEndpointBase<ListQualityRulesEndpoint>
{
}
