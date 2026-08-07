using Fdw.Services.Quality.Endpoints.Promotion;
using Fdw.Services.Quality.Services;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints.Promotion;

/// <summary>
/// Lists all configured deployment environments.
/// </summary>
public sealed class ListEnvironmentsEndpoint : ListEnvironmentsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListEnvironmentsEndpoint"/> class.
    /// </summary>
    public ListEnvironmentsEndpoint(IPromotionService promotionService, ILogger<ListEnvironmentsEndpointBase>? logger)
        : base(promotionService, logger) { }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint() => Tags("Promotions");
}
