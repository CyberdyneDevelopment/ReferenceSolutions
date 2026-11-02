using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceQuality.Endpoints.QualityRuleEndpointOptions;

/// <summary>The DeleteQualityRule endpoint.</summary>
[WebMcpTool("delete_quality_rule", "Delete quality rule.")]
[TypeOption(typeof(QualityRuleEndpoints), "DeleteQualityRule")]
public class DeleteQualityRuleOption : QualityRuleEndpointBase<DeleteQualityRuleEndpoint>
{
}
