using System.Diagnostics.CodeAnalysis;
using System;
using Fdw.Operations.Abstractions.Execution;
using Microsoft.Extensions.Logging;
using ReferenceExecution.Endpoints.Logging;

namespace ReferenceExecution.Endpoints;

/// <summary>
/// Endpoint to get execution details by ID.
/// Inherits default behavior from Tier 2 and adds Reference.Api logging.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetExecutionEndpoint : Fdw.Operations.Endpoints.Executions.GetExecutionEndpoint
{
    private readonly ILogger<GetExecutionEndpoint> _logger;

    public GetExecutionEndpoint(IExecutionTracker tracker, ILogger<GetExecutionEndpoint> logger)
        : base(tracker)
    {
        _logger = logger;
    }

    protected override void OnFetchingExecution(Guid id)
    {
        ExecutionLog.FetchingExecution(_logger, id);
    }

    protected override void OnExecutionFetchFailed(Guid id, string error)
    {
        ExecutionLog.ExecutionFetchFailed(_logger, id, error);
    }

    protected override void OnExecutionNotFound(Guid id)
    {
        ExecutionLog.ExecutionNotFound(_logger, id);
    }

    protected override void OnExecutionRetrieved(Guid id)
    {
        ExecutionLog.ExecutionRetrieved(_logger, id);
    }
}
