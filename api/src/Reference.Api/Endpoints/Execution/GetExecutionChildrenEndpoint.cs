using System.Diagnostics.CodeAnalysis;
using System;
using Fdw.Operations.Abstractions.Execution;
using Microsoft.Extensions.Logging;
using Reference.Api.Logging;

namespace Reference.Api.Endpoints;

/// <summary>
/// Endpoint to get children of an execution.
/// Inherits default behavior from Tier 2 and adds Reference.Api logging.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetExecutionChildrenEndpoint : Fdw.Operations.Endpoints.Executions.GetExecutionChildrenEndpoint
{
    private readonly ILogger<GetExecutionChildrenEndpoint> _logger;

    public GetExecutionChildrenEndpoint(IExecutionTracker tracker, ILogger<GetExecutionChildrenEndpoint> logger)
        : base(tracker)
    {
        _logger = logger;
    }

    protected override void OnFetchingChildren(Guid id)
    {
        ExecutionLog.FetchingChildren(_logger, id);
    }

    protected override void OnExecutionNotFound(Guid id)
    {
        ExecutionLog.ExecutionNotFound(_logger, id);
    }

    protected override void OnChildrenFetchFailed(Guid id, string error)
    {
        ExecutionLog.ChildrenFetchFailed(_logger, id, error);
    }

    protected override void OnChildrenRetrieved(Guid id, int count)
    {
        ExecutionLog.ChildrenRetrieved(_logger, id, count);
    }
}
