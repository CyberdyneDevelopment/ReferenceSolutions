using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Endpoints.Analytics;
using Fdw.Web.Analytics.Clients;
using Microsoft.Extensions.Logging;

namespace ReferenceAnalytics.Endpoints;

/// <summary>
/// Gets analytics summary for a time period.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetAnalyticsEndpoint : GetAnalyticsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetAnalyticsEndpoint"/> class.
    /// </summary>
    public GetAnalyticsEndpoint(
        IAnalyticsService analyticsService,
        ILogger<GetAnalyticsEndpoint>? logger = null)
        : base(analyticsService, logger!)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Analytics");
    }
}
