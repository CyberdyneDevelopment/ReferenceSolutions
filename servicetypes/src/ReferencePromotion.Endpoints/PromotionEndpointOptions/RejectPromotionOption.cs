using Fdw.Collections.Attributes;

namespace ReferencePromotion.Endpoints.PromotionEndpointOptions;

/// <summary>The RejectPromotion endpoint.</summary>
[TypeOption(typeof(PromotionEndpoints), "RejectPromotion")]
public class RejectPromotionOption : PromotionEndpointBase<RejectPromotionEndpoint>
{
}
