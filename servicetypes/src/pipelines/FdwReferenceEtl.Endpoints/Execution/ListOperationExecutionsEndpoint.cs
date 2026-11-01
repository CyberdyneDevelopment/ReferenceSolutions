using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Execution;
using Microsoft.Extensions.Logging;
using FdwReferenceEtl.Endpoints.Execution.Logging;
using Fdw.Operations.Endpoints.Executions;

namespace FdwReferenceEtl.Endpoints.Execution;

/// <summary>
/// Endpoint to list executions with pagination and filters.
/// Inherits default behavior from Tier 2 and adds Reference.Api logging.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListOperationExecutionsEndpoint : ListExecutionsEndpointBase
{
    private readonly ILogger<ListOperationExecutionsEndpoint> _logger;

    public ListOperationExecutionsEndpoint(IExecutionTracker tracker, ILogger<ListOperationExecutionsEndpoint> logger)
        : base(logger, tracker) {
        _logger = logger;
    }

    protected override void OnListingExecutions(int page, int pageSize)
    {
        ExecutionLog.ListingExecutions(_logger, page, pageSize);
    }

    protected override void OnQueryingByCorrelationId(string correlationId)
    {
        ExecutionLog.QueryingByCorrelationId(_logger, correlationId);
    }

    protected override void OnExecutionQueryFailed(string error)
    {
        ExecutionLog.ExecutionQueryFailed(_logger, error);
    }

    protected override void OnExecutionsListed(int count)
    {
        ExecutionLog.ExecutionsListed(_logger, count);
    }
}
