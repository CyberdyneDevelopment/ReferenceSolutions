using Fdw.Services.Quality.Endpoints.Promotion;
using Fdw.Services.Quality.Services;
using Microsoft.Extensions.Logging;

namespace ReferencePromotion.Endpoints;

/// <summary>
/// Approves a promotion request.
/// </summary>
public class ApprovePromotionEndpoint : ApprovePromotionEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApprovePromotionEndpoint"/> class.
    /// </summary>
    public ApprovePromotionEndpoint(IPromotionService promotionService, ILogger<ApprovePromotionEndpointBase>? logger)
        : base(promotionService, logger) { }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint() => Tags("Promotions");
}
