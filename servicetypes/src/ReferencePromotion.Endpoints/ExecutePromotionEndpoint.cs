using Fdw.Services.Quality.Endpoints.Promotion;
using Fdw.Services.Quality.Services;
using Microsoft.Extensions.Logging;

namespace ReferencePromotion.Endpoints;

/// <summary>
/// Executes an approved promotion request.
/// </summary>
public class ExecutePromotionEndpoint : ExecutePromotionEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutePromotionEndpoint"/> class.
    /// </summary>
    public ExecutePromotionEndpoint(IPromotionService promotionService, ILogger<ExecutePromotionEndpointBase>? logger)
        : base(promotionService, logger) { }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint() => Tags("Promotions");
}
