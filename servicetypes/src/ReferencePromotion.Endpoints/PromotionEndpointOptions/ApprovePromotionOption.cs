using Fdw.Collections.Attributes;

namespace ReferencePromotion.Endpoints.PromotionEndpointOptions;

/// <summary>The ApprovePromotion endpoint.</summary>
[TypeOption(typeof(PromotionEndpoints), "ApprovePromotion")]
public class ApprovePromotionOption : PromotionEndpointBase<ApprovePromotionEndpoint>
{
}
