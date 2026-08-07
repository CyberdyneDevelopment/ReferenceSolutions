using Fdw.Operations.Endpoints.Health;
using Fdw.Services.Abstractions.Health.Monitoring;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Reference.Api.Endpoints.Health;

/// <summary>
/// Gets health check history for a service over a specified time window.
/// </summary>
public sealed class GetServiceHealthHistoryEndpoint : GetServiceHealthHistoryEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetServiceHealthHistoryEndpoint"/> class.
    /// </summary>
    public GetServiceHealthHistoryEndpoint(IHealthMonitorProvider monitors, IOptions<HealthMonitorSelectionOptions> selection, ILogger<GetServiceHealthHistoryEndpointBase>? logger)
        : base(monitors, selection, logger) { }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint() => Tags("Health");
}
