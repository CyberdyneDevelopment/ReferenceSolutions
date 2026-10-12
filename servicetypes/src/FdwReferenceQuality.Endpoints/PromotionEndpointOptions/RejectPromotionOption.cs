using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePromotion.Endpoints.PromotionEndpointOptions;

/// <summary>The RejectPromotion endpoint.</summary>
[WebMcpTool("reject_promotion", "Reject promotion.")]
[TypeOption(typeof(PromotionEndpoints), "RejectPromotion")]
public class RejectPromotionOption : PromotionEndpointBase<RejectPromotionEndpoint>
{
}
