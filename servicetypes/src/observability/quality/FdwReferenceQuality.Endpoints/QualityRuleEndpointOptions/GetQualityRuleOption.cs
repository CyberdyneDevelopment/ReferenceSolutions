using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceQuality.Endpoints.QualityRuleEndpointOptions;

/// <summary>The GetQualityRule endpoint.</summary>
[WebMcpTool("get_quality_rule", "Get quality rule.", ReadOnly = true)]
[TypeOption(typeof(QualityRuleEndpoints), "GetQualityRule")]
public class GetQualityRuleOption : QualityRuleEndpointBase<GetQualityRuleEndpoint>
{
}
