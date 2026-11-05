using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceEtl.Endpoints.Execution;

/// <summary>
/// Endpoint to get what a run did (FDW-3381): rows read, written and rejected, the size moved and the run time of the run, each stage and each component.
/// Inherits the behavior from the framework.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetExecutionTrackingEndpoint : GetExecutionTrackingEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="GetExecutionTrackingEndpoint"/> class.</summary>
    public GetExecutionTrackingEndpoint(IExecutionTracker tracker, ILogger<GetExecutionTrackingEndpoint> logger)
        : base(logger, tracker)
    {
    }
}
