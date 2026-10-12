using Fdw.Services.Abstractions;
using Fdw.Operations.Endpoints.Health;
using Fdw.Services.Abstractions.Health.Monitoring;
using Microsoft.Extensions.Logging;

namespace FdwReferenceHealth.Endpoints;

/// <summary>
/// Gets the current health snapshot for the entire system.
/// </summary>
public sealed class GetSystemHealthEndpoint : GetSystemHealthEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetSystemHealthEndpoint"/> class.
    /// </summary>
    public GetSystemHealthEndpoint(IHealthMonitorProvider monitors, IHealthMonitorConfigurationProvider healthMonitors, ILogger<GetSystemHealthEndpointBase>? logger)
        : base(monitors, healthMonitors, logger) { }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint() => Tags("Health");
}
