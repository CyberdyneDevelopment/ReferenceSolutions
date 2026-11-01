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
/// Proxy endpoint to delete a schedule on SchedulerServer.
/// DELETE /api/v1/schedules/{name} -> SchedulerServer DELETE /api/v1/schedules/{name}
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteScheduleProxyEndpoint : Endpoint<ScheduleNameProxyRequest>
{
    private readonly IScheduleClient _client;
    private readonly ILogger<DeleteScheduleProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="DeleteScheduleProxyEndpoint"/> class.</summary>
    /// <param name="client">The schedule client.</param>
    /// <param name="logger">The logger.</param>
    public DeleteScheduleProxyEndpoint(IScheduleClient client, ILogger<DeleteScheduleProxyEndpoint> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Delete("/schedules/{name}");
        Policies("schedules:delete");
        Summary(s =>
        {
            s.Summary = "Delete schedule (proxy)";
            s.Description = "Proxies the request to SchedulerServer to delete a schedule.";
        });
        Tags("Schedules");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ScheduleNameProxyRequest req, CancellationToken ct)
    {
        ProxyLog.ProxyRequest(_logger, "DELETE", "Scheduler", $"schedules/{req.Name}");

        var result = await _client.DeleteSchedule(req.Name, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            ProxyLog.ProxyFailed(_logger, "Scheduler", result.CurrentMessage);
            var (status, problem) = ResultHttpStatusMapper.Map(result, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: status)).ConfigureAwait(false);
            return;
        }

        ProxyLog.ProxyResponse(_logger, "Scheduler", 204);
        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
