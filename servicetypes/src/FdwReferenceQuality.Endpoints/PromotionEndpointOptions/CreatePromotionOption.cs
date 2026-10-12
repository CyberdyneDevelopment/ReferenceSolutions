using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePromotion.Endpoints.PromotionEndpointOptions;

/// <summary>The CreatePromotion endpoint.</summary>
[WebMcpTool("create_promotion", "Create promotion.")]
[TypeOption(typeof(PromotionEndpoints), "CreatePromotion")]
public class CreatePromotionOption : PromotionEndpointBase<CreatePromotionEndpoint>
{
}
