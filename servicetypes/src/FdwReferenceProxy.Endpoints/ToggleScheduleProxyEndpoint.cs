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
/// Proxy endpoint to flip a schedule's enabled state on SchedulerServer.
/// POST /api/v1/schedules/{name}/toggle -> SchedulerServer POST /api/v1/schedules/{name}/toggle
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ToggleScheduleProxyEndpoint : Endpoint<ScheduleNameProxyRequest, ScheduleInfoDto>
{
    private readonly IScheduleClient _client;
    private readonly ILogger<ToggleScheduleProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="ToggleScheduleProxyEndpoint"/> class.</summary>
    /// <param name="client">The schedule client.</param>
    /// <param name="logger">The logger.</param>
    public ToggleScheduleProxyEndpoint(IScheduleClient client, ILogger<ToggleScheduleProxyEndpoint> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/schedules/{name}/toggle");
        Policies("schedules:write");
        Summary(s =>
        {
            s.Summary = "Toggle schedule (proxy)";
            s.Description = "Proxies the request to SchedulerServer to enable or disable a schedule.";
        });
        Tags("Schedules");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ScheduleNameProxyRequest req, CancellationToken ct)
    {
        ProxyLog.ProxyRequest(_logger, "POST", "Scheduler", $"schedules/{req.Name}/toggle");

        var result = await _client.ToggleSchedule(req.Name, ct).ConfigureAwait(false);
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
