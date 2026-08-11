using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Scheduling.Clients.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ReferenceProxy.Endpoints.Logging;

namespace ReferenceProxy.Endpoints;

/// <summary>
/// Proxy endpoint to list schedules from SchedulerServer.
/// GET /api/proxy/schedules → SchedulerServer GET /api/v1/schedules
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetSchedulesProxyEndpoint : EndpointWithoutRequest<IReadOnlyList<ScheduleInfoDto>>
{
    private readonly IScheduleClient _client;
    private readonly ILogger<GetSchedulesProxyEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSchedulesProxyEndpoint"/> class.
    /// </summary>
    public GetSchedulesProxyEndpoint(IScheduleClient client, ILogger<GetSchedulesProxyEndpoint> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/proxy/schedules");
        Policies("schedules:read");
        Summary(s =>
        {
            s.Summary = "List schedules (proxy)";
            s.Description = "Proxies the request to SchedulerServer to list schedules.";
        });
        Tags("Schedules");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(CancellationToken ct)
    {
        ProxyLog.ProxyRequest(_logger, "GET", "Scheduler", "schedules");

        var result = await _client.List(ct);

        if (!result.IsSuccess)
        {
            var errorMessage = result.CurrentMessage ?? "Proxy request failed";
            ProxyLog.ProxyFailed(_logger, "Scheduler", errorMessage);
            AddError("Failed to proxy request to SchedulerServer");
            await Send.ErrorsAsync(502, ct);
            return;
        }

        ProxyLog.ProxyResponse(_logger, "Scheduler", 200);
        await Send.OkAsync(result.Value!, ct);
    }
}
