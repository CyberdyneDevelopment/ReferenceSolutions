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
/// Proxy endpoint to read one schedule from SchedulerServer.
/// GET /api/v1/schedules/{name} -> SchedulerServer GET /api/v1/schedules/{name}
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetScheduleProxyEndpoint : Endpoint<ScheduleNameProxyRequest, ScheduleInfoDto>
{
    private readonly IScheduleClient _client;
    private readonly ILogger<GetScheduleProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="GetScheduleProxyEndpoint"/> class.</summary>
    /// <param name="client">The schedule client.</param>
    /// <param name="logger">The logger.</param>
    public GetScheduleProxyEndpoint(IScheduleClient client, ILogger<GetScheduleProxyEndpoint> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/schedules/{name}");
        Policies("schedules:read");
        Summary(s =>
        {
            s.Summary = "Get schedule (proxy)";
            s.Description = "Proxies the request to SchedulerServer, which owns the schedule.";
        });
        Tags("Schedules");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ScheduleNameProxyRequest req, CancellationToken ct)
    {
        ProxyLog.ProxyRequest(_logger, "GET", "Scheduler", $"schedules/{req.Name}");

        var result = await _client.Get(req.Name, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            ProxyLog.ProxyFailed(_logger, "Scheduler", result.CurrentMessage);
            var (status, problem) = ResultHttpStatusMapper.Map(result, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: status)).ConfigureAwait(false);
            return;
        }

        if (result.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        ProxyLog.ProxyResponse(_logger, "Scheduler", 200);
        await Send.OkAsync(result.Value, ct).ConfigureAwait(false);
    }
}
