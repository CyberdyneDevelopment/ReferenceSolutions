using Fdw.Operations.Endpoints.Health;
using Fdw.Services.Abstractions.Health.Monitoring;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Reference.Api.Endpoints.Health;

/// <summary>
/// Gets the current health snapshot for the entire system.
/// </summary>
public sealed class GetSystemHealthEndpoint : GetSystemHealthEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetSystemHealthEndpoint"/> class.
    /// </summary>
    public GetSystemHealthEndpoint(IHealthMonitorProvider monitors, IOptions<HealthMonitorSelectionOptions> selection, ILogger<GetSystemHealthEndpointBase>? logger)
        : base(monitors, selection, logger) { }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint() => Tags("Health");
}
