using Fdw.Collections.Attributes;

namespace ReferencePromotion.Endpoints.PromotionEndpointOptions;

/// <summary>The ListPromotions endpoint.</summary>
[TypeOption(typeof(PromotionEndpoints), "ListPromotions")]
public class ListPromotionsOption : PromotionEndpointBase<ListPromotionsEndpoint>
{
}
