using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferencePromotion.Endpoints.PromotionEndpointOptions;

/// <summary>The ListPromotions endpoint.</summary>
[WebMcpTool("list_promotions", "List promotions.", ReadOnly = true)]
[TypeOption(typeof(PromotionEndpoints), "ListPromotions")]
public class ListPromotionsOption : PromotionEndpointBase<ListPromotionsEndpoint>
{
}
