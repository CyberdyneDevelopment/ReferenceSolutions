using Fdw.Operations;
using Fdw.Operations.Endpoints.Thresholds;
using Microsoft.Extensions.Logging;

namespace FdwReferenceThresholds.Endpoints;

/// <summary>
/// Attaches a threshold to a subject.
/// </summary>
public sealed class AttachThresholdEndpoint : AttachThresholdEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AttachThresholdEndpoint"/> class.
    /// </summary>
    /// <param name="links">The threshold links.</param>
    /// <param name="logger">The logger.</param>
    public AttachThresholdEndpoint(IThresholdLinks links, ILogger<AttachThresholdEndpointBase>? logger)
        : base(links, logger) { }
}
