using System.Diagnostics.CodeAnalysis;
using System;
using Fdw.Operations.Abstractions.Execution;
using Microsoft.Extensions.Logging;
using FdwReferenceEtl.Endpoints.Execution.Logging;
using Fdw.Operations.Endpoints.Executions;

namespace FdwReferenceEtl.Endpoints.Execution;

/// <summary>
/// Endpoint to get events for an execution.
/// Inherits default behavior from Tier 2 and adds Reference.Api logging.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetOperationExecutionEventsEndpoint : GetExecutionEventsEndpointBase
{
    private readonly ILogger<GetOperationExecutionEventsEndpoint> _logger;

    public GetOperationExecutionEventsEndpoint(IExecutionTracker tracker, ILogger<GetOperationExecutionEventsEndpoint> logger)
        : base(logger, tracker) {
        _logger = logger;
    }

    protected override void OnFetchingEvents(Guid id)
    {
        ExecutionLog.FetchingEvents(_logger, id);
    }

    protected override void OnExecutionNotFound(Guid id)
    {
        ExecutionLog.ExecutionNotFound(_logger, id);
    }

    protected override void OnEventsFetchFailed(Guid id, string error)
    {
        ExecutionLog.EventsFetchFailed(_logger, id, error);
    }

    protected override void OnEventsRetrieved(Guid id, int count)
    {
        ExecutionLog.EventsRetrieved(_logger, id, count);
    }
}
