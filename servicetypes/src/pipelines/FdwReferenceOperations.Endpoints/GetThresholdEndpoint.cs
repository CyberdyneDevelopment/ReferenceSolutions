using Fdw.Operations;
using Fdw.Operations.Endpoints.Thresholds;
using Microsoft.Extensions.Logging;

namespace FdwReferenceThresholds.Endpoints;

/// <summary>
/// Reads one threshold with its rules.
/// </summary>
public sealed class GetThresholdEndpoint : GetThresholdEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetThresholdEndpoint"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="thresholds">The thresholds.</param>
    public GetThresholdEndpoint(ILogger<GetThresholdEndpointBase> logger, IThresholdConfigurationProvider thresholds)
        : base(logger, thresholds) { }
}
