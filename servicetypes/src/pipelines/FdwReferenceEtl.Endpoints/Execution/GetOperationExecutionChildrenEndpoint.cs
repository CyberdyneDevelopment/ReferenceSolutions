using System.Diagnostics.CodeAnalysis;
using System;
using Fdw.Operations.Abstractions.Execution;
using Microsoft.Extensions.Logging;
using FdwReferenceEtl.Endpoints.Execution.Logging;
using Fdw.Operations.Endpoints.Executions;

namespace FdwReferenceEtl.Endpoints.Execution;

/// <summary>
/// Endpoint to get children of an execution.
/// Inherits default behavior from Tier 2 and adds Reference.Api logging.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetOperationExecutionChildrenEndpoint : GetExecutionChildrenEndpointBase
{
    private readonly ILogger<GetOperationExecutionChildrenEndpoint> _logger;

    public GetOperationExecutionChildrenEndpoint(IExecutionTracker tracker, ILogger<GetOperationExecutionChildrenEndpoint> logger)
        : base(logger, tracker) {
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
