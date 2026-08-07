using Fdw.Operations.Endpoints.Health;
using Fdw.Services.Abstractions.Health.Monitoring;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Reference.Api.Endpoints.Health;

/// <summary>
/// Gets the current health snapshot for a specific service.
/// </summary>
public sealed class GetServiceHealthEndpoint : GetServiceHealthEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetServiceHealthEndpoint"/> class.
    /// </summary>
    public GetServiceHealthEndpoint(IHealthMonitorProvider monitors, IOptions<HealthMonitorSelectionOptions> selection, ILogger<GetServiceHealthEndpointBase>? logger)
        : base(monitors, selection, logger) { }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint() => Tags("Health");
}
