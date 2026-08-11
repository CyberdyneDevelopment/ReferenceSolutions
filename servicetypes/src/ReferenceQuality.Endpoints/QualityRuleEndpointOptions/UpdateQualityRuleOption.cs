using Fdw.Collections.Attributes;

namespace ReferenceQuality.Endpoints.QualityRuleEndpointOptions;

/// <summary>The UpdateQualityRule endpoint.</summary>
[TypeOption(typeof(QualityRuleEndpoints), "UpdateQualityRule")]
public class UpdateQualityRuleOption : QualityRuleEndpointBase<UpdateQualityRuleEndpoint>
{
}
