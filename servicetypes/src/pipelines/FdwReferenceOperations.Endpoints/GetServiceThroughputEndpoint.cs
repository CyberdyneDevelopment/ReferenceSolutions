using Fdw.Services.Abstractions;
using Fdw.Operations.Endpoints.Health;
using Fdw.Services.Abstractions.Health.Monitoring;
using Microsoft.Extensions.Logging;

namespace FdwReferenceHealth.Endpoints;

/// <summary>
/// Gets throughput data for a service over a specified time window.
/// </summary>
public sealed class GetServiceThroughputEndpoint : GetServiceThroughputEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetServiceThroughputEndpoint"/> class.
    /// </summary>
    public GetServiceThroughputEndpoint(IHealthMonitorProvider monitors, IHealthMonitorConfigurationProvider healthMonitors, ILogger<GetServiceThroughputEndpointBase>? logger)
        : base(monitors, healthMonitors, logger) { }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint() => Tags("Health");
}
