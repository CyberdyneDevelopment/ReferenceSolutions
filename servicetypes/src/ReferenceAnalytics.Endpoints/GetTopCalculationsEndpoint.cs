using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Endpoints.Analytics;
using Fdw.Web.Analytics.Clients;
using Microsoft.Extensions.Logging;

namespace ReferenceAnalytics.Endpoints;

/// <summary>
/// Gets top calculations by usage.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetTopCalculationsEndpoint : GetTopCalculationsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetTopCalculationsEndpoint"/> class.
    /// </summary>
    public GetTopCalculationsEndpoint(
        IAnalyticsService analyticsService,
        ILogger<GetTopCalculationsEndpoint>? logger = null)
        : base(analyticsService, logger!)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Analytics");
    }
}
