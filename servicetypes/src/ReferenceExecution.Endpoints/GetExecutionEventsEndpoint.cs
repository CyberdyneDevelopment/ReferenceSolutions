using System.Diagnostics.CodeAnalysis;
using System;
using Fdw.Operations.Abstractions.Execution;
using Microsoft.Extensions.Logging;
using ReferenceExecution.Endpoints.Logging;

namespace ReferenceExecution.Endpoints;

/// <summary>
/// Endpoint to get events for an execution.
/// Inherits default behavior from Tier 2 and adds Reference.Api logging.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetExecutionEventsEndpoint : Fdw.Operations.Endpoints.Executions.GetExecutionEventsEndpoint
{
    private readonly ILogger<GetExecutionEventsEndpoint> _logger;

    public GetExecutionEventsEndpoint(IExecutionTracker tracker, ILogger<GetExecutionEventsEndpoint> logger)
        : base(tracker)
    {
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
