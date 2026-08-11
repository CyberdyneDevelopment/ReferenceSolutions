using Fdw.Collections.Attributes;

namespace ReferenceQuality.Endpoints.QualityRuleEndpointOptions;

/// <summary>The DeleteQualityRule endpoint.</summary>
[TypeOption(typeof(QualityRuleEndpoints), "DeleteQualityRule")]
public class DeleteQualityRuleOption : QualityRuleEndpointBase<DeleteQualityRuleEndpoint>
{
}
