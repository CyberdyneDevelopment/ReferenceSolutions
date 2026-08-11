using Fdw.Collections.Attributes;

namespace ReferencePromotion.Endpoints.PromotionEndpointOptions;

/// <summary>The ExecutePromotion endpoint.</summary>
[TypeOption(typeof(PromotionEndpoints), "ExecutePromotion")]
public class ExecutePromotionOption : PromotionEndpointBase<ExecutePromotionEndpoint>
{
}
