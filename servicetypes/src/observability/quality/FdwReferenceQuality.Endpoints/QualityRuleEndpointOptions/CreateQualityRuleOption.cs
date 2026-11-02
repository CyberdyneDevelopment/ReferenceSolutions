using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceQuality.Endpoints.QualityRuleEndpointOptions;

/// <summary>The CreateQualityRule endpoint.</summary>
[WebMcpTool("create_quality_rule", "Create quality rule.")]
[TypeOption(typeof(QualityRuleEndpoints), "CreateQualityRule")]
public class CreateQualityRuleOption : QualityRuleEndpointBase<CreateQualityRuleEndpoint>
{
}
