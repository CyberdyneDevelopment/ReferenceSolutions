using Fdw.Operations.Endpoints.Health;
using Fdw.Services.Abstractions.Health.Monitoring;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Reference.Api.Endpoints.Health;

/// <summary>
/// Gets throughput data for a service over a specified time window.
/// </summary>
public sealed class GetServiceThroughputEndpoint : GetServiceThroughputEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetServiceThroughputEndpoint"/> class.
    /// </summary>
    public GetServiceThroughputEndpoint(IHealthMonitorProvider monitors, IOptions<HealthMonitorSelectionOptions> selection, ILogger<GetServiceThroughputEndpointBase>? logger)
        : base(monitors, selection, logger) { }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint() => Tags("Health");
}
