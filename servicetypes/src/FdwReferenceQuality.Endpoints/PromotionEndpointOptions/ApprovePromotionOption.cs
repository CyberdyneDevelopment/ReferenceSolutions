using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePromotion.Endpoints.PromotionEndpointOptions;

/// <summary>The ApprovePromotion endpoint.</summary>
[WebMcpTool("approve_promotion", "Approve promotion.")]
[TypeOption(typeof(PromotionEndpoints), "ApprovePromotion")]
public class ApprovePromotionOption : PromotionEndpointBase<ApprovePromotionEndpoint>
{
}
