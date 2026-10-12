using Fdw.Services.Abstractions;
using Fdw.Operations.Endpoints.Health;
using Fdw.Services.Abstractions.Health.Monitoring;
using Microsoft.Extensions.Logging;

namespace FdwReferenceHealth.Endpoints;

/// <summary>
/// Gets the current health snapshot for a specific service.
/// </summary>
public sealed class GetServiceHealthEndpoint : GetServiceHealthEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetServiceHealthEndpoint"/> class.
    /// </summary>
    public GetServiceHealthEndpoint(IHealthMonitorProvider monitors, IHealthMonitorConfigurationProvider healthMonitors, ILogger<GetServiceHealthEndpointBase>? logger)
        : base(monitors, healthMonitors, logger) { }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint() => Tags("Health");
}
