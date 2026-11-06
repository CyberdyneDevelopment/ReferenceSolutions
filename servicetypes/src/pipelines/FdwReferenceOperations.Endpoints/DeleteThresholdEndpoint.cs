using Fdw.Operations;
using Fdw.Operations.Endpoints.Thresholds;
using Microsoft.Extensions.Logging;

namespace FdwReferenceThresholds.Endpoints;

/// <summary>
/// Deletes a threshold.
/// </summary>
public sealed class DeleteThresholdEndpoint : DeleteThresholdEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteThresholdEndpoint"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="thresholds">The thresholds.</param>
    public DeleteThresholdEndpoint(ILogger<DeleteThresholdEndpointBase> logger, IThresholdConfigurationProvider thresholds)
        : base(logger, thresholds) { }
}
