using Fdw.Services.Quality.Endpoints.Promotion;
using Fdw.Services.Quality.Services;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints.Promotion;

/// <summary>
/// Rejects a promotion request.
/// </summary>
public sealed class RejectPromotionEndpoint : RejectPromotionEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RejectPromotionEndpoint"/> class.
    /// </summary>
    public RejectPromotionEndpoint(IPromotionService promotionService, ILogger<RejectPromotionEndpointBase>? logger)
        : base(promotionService, logger) { }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint() => Tags("Promotions");
}
