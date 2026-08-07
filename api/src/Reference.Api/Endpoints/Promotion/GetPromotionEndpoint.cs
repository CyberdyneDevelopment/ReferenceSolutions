using Fdw.Services.Quality.Endpoints.Promotion;
using Fdw.Services.Quality.Services;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints.Promotion;

/// <summary>
/// Retrieves a promotion request by identifier.
/// </summary>
public sealed class GetPromotionEndpoint : GetPromotionEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetPromotionEndpoint"/> class.
    /// </summary>
    public GetPromotionEndpoint(IPromotionService promotionService, ILogger<GetPromotionEndpointBase>? logger)
        : base(promotionService, logger) { }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint() => Tags("Promotions");
}
