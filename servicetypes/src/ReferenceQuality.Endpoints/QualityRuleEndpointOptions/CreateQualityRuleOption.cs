using Fdw.Collections.Attributes;

namespace ReferenceQuality.Endpoints.QualityRuleEndpointOptions;

/// <summary>The CreateQualityRule endpoint.</summary>
[TypeOption(typeof(QualityRuleEndpoints), "CreateQualityRule")]
public class CreateQualityRuleOption : QualityRuleEndpointBase<CreateQualityRuleEndpoint>
{
}
