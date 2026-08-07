using Fdw.Services.Quality.Endpoints.Promotion;
using Fdw.Services.Quality.Services;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints.Promotion;

/// <summary>
/// Creates a new promotion request.
/// </summary>
public sealed class CreatePromotionEndpoint : CreatePromotionEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreatePromotionEndpoint"/> class.
    /// </summary>
    public CreatePromotionEndpoint(IPromotionService promotionService, ILogger<CreatePromotionEndpointBase>? logger)
        : base(promotionService, logger) { }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint() => Tags("Promotions");
}
