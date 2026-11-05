using Fdw.Operations;
using Fdw.Operations.Endpoints.Thresholds;
using Microsoft.Extensions.Logging;

namespace FdwReferenceThresholds.Endpoints;

/// <summary>
/// Lists the thresholds, one page at a time.
/// </summary>
public sealed class ListThresholdsEndpoint : ListThresholdsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListThresholdsEndpoint"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="thresholds">The thresholds.</param>
    public ListThresholdsEndpoint(ILogger<ListThresholdsEndpointBase> logger, IThresholdConfigurationProvider thresholds)
        : base(logger, thresholds) { }
}
