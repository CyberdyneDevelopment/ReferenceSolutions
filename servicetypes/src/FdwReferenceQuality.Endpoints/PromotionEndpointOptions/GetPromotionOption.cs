using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePromotion.Endpoints.PromotionEndpointOptions;

/// <summary>The GetPromotion endpoint.</summary>
[WebMcpTool("get_promotion", "Get promotion.", ReadOnly = true)]
[TypeOption(typeof(PromotionEndpoints), "GetPromotion")]
public class GetPromotionOption : PromotionEndpointBase<GetPromotionEndpoint>
{
}
