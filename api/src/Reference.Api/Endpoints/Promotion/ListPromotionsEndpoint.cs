using Fdw.Services.Quality.Endpoints.Promotion;
using Fdw.Services.Quality.Services;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints.Promotion;

/// <summary>
/// Lists promotion requests, optionally filtered by status.
/// </summary>
public sealed class ListPromotionsEndpoint : ListPromotionsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListPromotionsEndpoint"/> class.
    /// </summary>
    public ListPromotionsEndpoint(IPromotionService promotionService, ILogger<ListPromotionsEndpointBase>? logger)
        : base(promotionService, logger) { }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint() => Tags("Promotions");
}
