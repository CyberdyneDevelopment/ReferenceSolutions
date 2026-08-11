using Fdw.Collections.Attributes;

namespace ReferencePromotion.Endpoints.PromotionEndpointOptions;

/// <summary>The GetPromotion endpoint.</summary>
[TypeOption(typeof(PromotionEndpoints), "GetPromotion")]
public class GetPromotionOption : PromotionEndpointBase<GetPromotionEndpoint>
{
}
