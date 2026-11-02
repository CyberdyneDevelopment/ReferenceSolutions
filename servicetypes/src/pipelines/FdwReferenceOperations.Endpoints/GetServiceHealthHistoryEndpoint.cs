using Fdw.Services.Abstractions;
using Fdw.Operations.Endpoints.Health;
using Fdw.Services.Abstractions.Health.Monitoring;
using Microsoft.Extensions.Logging;

namespace FdwReferenceHealth.Endpoints;

/// <summary>
/// Gets health check history for a service over a specified time window.
/// </summary>
public sealed class GetServiceHealthHistoryEndpoint : GetServiceHealthHistoryEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetServiceHealthHistoryEndpoint"/> class.
    /// </summary>
    public GetServiceHealthHistoryEndpoint(IHealthMonitorProvider monitors, IHealthMonitorConfigurationProvider healthMonitors, ILogger<GetServiceHealthHistoryEndpointBase>? logger)
        : base(monitors, healthMonitors, logger) { }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint() => Tags("Health");
}
