using Fdw.Operations;
using Fdw.Operations.Endpoints.Thresholds;
using Microsoft.Extensions.Logging;

namespace FdwReferenceThresholds.Endpoints;

/// <summary>
/// Lists the thresholds attached to a subject.
/// </summary>
public sealed class GetSubjectThresholdsEndpoint : GetSubjectThresholdsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetSubjectThresholdsEndpoint"/> class.
    /// </summary>
    /// <param name="links">The threshold links.</param>
    /// <param name="logger">The logger.</param>
    public GetSubjectThresholdsEndpoint(IThresholdLinks links, ILogger<GetSubjectThresholdsEndpointBase>? logger)
        : base(links, logger) { }
}
