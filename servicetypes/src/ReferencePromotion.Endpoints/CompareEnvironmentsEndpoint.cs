using Fdw.Services.Quality.Endpoints.Promotion;
using Fdw.Services.Quality.Services;
using Microsoft.Extensions.Logging;

namespace ReferencePromotion.Endpoints;

/// <summary>
/// Compares configuration between two environments for a given entity.
/// </summary>
public class CompareEnvironmentsEndpoint : CompareEnvironmentsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompareEnvironmentsEndpoint"/> class.
    /// </summary>
    public CompareEnvironmentsEndpoint(IPromotionService promotionService, ILogger<CompareEnvironmentsEndpointBase>? logger)
        : base(promotionService, logger) { }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint() => Tags("Promotions");
}
