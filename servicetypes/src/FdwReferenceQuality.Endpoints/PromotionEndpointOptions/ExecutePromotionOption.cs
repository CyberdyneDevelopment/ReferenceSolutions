using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePromotion.Endpoints.PromotionEndpointOptions;

/// <summary>The ExecutePromotion endpoint.</summary>
[WebMcpTool("execute_promotion", "Execute promotion.")]
[TypeOption(typeof(PromotionEndpoints), "ExecutePromotion")]
public class ExecutePromotionOption : PromotionEndpointBase<ExecutePromotionEndpoint>
{
}
