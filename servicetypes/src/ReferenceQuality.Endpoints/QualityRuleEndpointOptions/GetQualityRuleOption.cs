using Fdw.Collections.Attributes;

namespace ReferenceQuality.Endpoints.QualityRuleEndpointOptions;

/// <summary>The GetQualityRule endpoint.</summary>
[TypeOption(typeof(QualityRuleEndpoints), "GetQualityRule")]
public class GetQualityRuleOption : QualityRuleEndpointBase<GetQualityRuleEndpoint>
{
}
