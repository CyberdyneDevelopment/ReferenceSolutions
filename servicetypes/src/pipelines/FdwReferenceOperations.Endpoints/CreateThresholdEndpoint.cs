using Fdw.Operations;
using Fdw.Operations.Endpoints.Thresholds;
using Microsoft.Extensions.Logging;

namespace FdwReferenceThresholds.Endpoints;

/// <summary>
/// Creates a threshold with its rules.
/// </summary>
public sealed class CreateThresholdEndpoint : CreateThresholdEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateThresholdEndpoint"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="thresholds">The thresholds.</param>
    public CreateThresholdEndpoint(ILogger<CreateThresholdEndpointBase> logger, IThresholdConfigurationProvider thresholds)
        : base(logger, thresholds) { }
}
