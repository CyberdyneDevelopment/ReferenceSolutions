using Fdw.Operations;
using Fdw.Operations.Endpoints.Thresholds;
using Microsoft.Extensions.Logging;

namespace FdwReferenceThresholds.Endpoints;

/// <summary>
/// Replaces a threshold and its rules.
/// </summary>
public sealed class UpdateThresholdEndpoint : UpdateThresholdEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateThresholdEndpoint"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="thresholds">The thresholds.</param>
    public UpdateThresholdEndpoint(ILogger<UpdateThresholdEndpointBase> logger, IThresholdConfigurationProvider thresholds)
        : base(logger, thresholds) { }
}
