using Fdw.Collections.Attributes;

namespace ReferencePromotion.Endpoints.PromotionEndpointOptions;

/// <summary>The CreatePromotion endpoint.</summary>
[TypeOption(typeof(PromotionEndpoints), "CreatePromotion")]
public class CreatePromotionOption : PromotionEndpointBase<CreatePromotionEndpoint>
{
}
