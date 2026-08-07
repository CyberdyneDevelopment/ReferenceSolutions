using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Execution;
using Microsoft.Extensions.Logging;
using Reference.Api.Logging;

namespace Reference.Api.Endpoints;

/// <summary>
/// Endpoint to list executions with pagination and filters.
/// Inherits default behavior from Tier 2 and adds Reference.Api logging.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListExecutionsEndpoint : Fdw.Operations.Endpoints.Executions.ListExecutionsEndpoint
{
    private readonly ILogger<ListExecutionsEndpoint> _logger;

    public ListExecutionsEndpoint(IExecutionTracker tracker, ILogger<ListExecutionsEndpoint> logger)
        : base(tracker)
    {
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
