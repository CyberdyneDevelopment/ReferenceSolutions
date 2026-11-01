using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceQuality.Endpoints.QualityRuleEndpointOptions;

/// <summary>The UpdateQualityRule endpoint.</summary>
[WebMcpTool("update_quality_rule", "Update quality rule.")]
[TypeOption(typeof(QualityRuleEndpoints), "UpdateQualityRule")]
public class UpdateQualityRuleOption : QualityRuleEndpointBase<UpdateQualityRuleEndpoint>
{
}
