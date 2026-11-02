using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Scheduling.Clients.Abstractions;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FdwReferenceProxy.Endpoints.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>
/// Proxy endpoint listing the schedule kinds SchedulerServer accepts.
/// GET /api/v1/schedules/types -> SchedulerServer GET /api/v1/schedules/types
/// </summary>
/// <remarks>
/// The literal segment is matched ahead of the name parameter on the sibling get route, so this
/// does not shadow a schedule that happens to be called "types".
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class GetScheduleTypesProxyEndpoint : EndpointWithoutRequest<IReadOnlyList<ScheduleTypeSummary>>
{
    private readonly IScheduleClient _client;
    private readonly ILogger<GetScheduleTypesProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="GetScheduleTypesProxyEndpoint"/> class.</summary>
    /// <param name="client">The schedule client.</param>
    /// <param name="logger">The logger.</param>
    public GetScheduleTypesProxyEndpoint(IScheduleClient client, ILogger<GetScheduleTypesProxyEndpoint> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/schedules/types");
        Policies("schedules:read");
        Summary(s =>
        {
            s.Summary = "List schedule types (proxy)";
            s.Description = "Proxies the request to SchedulerServer, which owns the schedule type collection.";
        });
        Tags("Schedules");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(CancellationToken ct)
    {
        ProxyLog.ProxyRequest(_logger, "GET", "Scheduler", "schedules/types");

        var result = await _client.GetScheduleTypes(ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            ProxyLog.ProxyFailed(_logger, "Scheduler", result.CurrentMessage);
            var (status, problem) = ResultHttpStatusMapper.Map(result, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: status)).ConfigureAwait(false);
            return;
        }

        ProxyLog.ProxyResponse(_logger, "Scheduler", 200);
        await Send.OkAsync(result.Value!, ct).ConfigureAwait(false);
    }
}
