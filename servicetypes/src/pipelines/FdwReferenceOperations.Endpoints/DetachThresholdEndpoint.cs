using Fdw.Operations;
using Fdw.Operations.Endpoints.Thresholds;
using Microsoft.Extensions.Logging;

namespace FdwReferenceThresholds.Endpoints;

/// <summary>
/// Detaches a threshold from a subject.
/// </summary>
public sealed class DetachThresholdEndpoint : DetachThresholdEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DetachThresholdEndpoint"/> class.
    /// </summary>
    /// <param name="links">The threshold links.</param>
    /// <param name="logger">The logger.</param>
    public DetachThresholdEndpoint(IThresholdLinks links, ILogger<DetachThresholdEndpointBase>? logger)
        : base(links, logger) { }
}
