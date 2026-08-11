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
/// Proxy endpoint to create a schedule on SchedulerServer.
/// POST /api/proxy/schedules → SchedulerServer POST /api/v1/schedules
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateScheduleProxyEndpoint : Endpoint<ProxyCreateScheduleRequest, CreateScheduleClientResponse>
{
    private readonly IScheduleClient _client;
    private readonly ILogger<CreateScheduleProxyEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateScheduleProxyEndpoint"/> class.
    /// </summary>
    public CreateScheduleProxyEndpoint(IScheduleClient client, ILogger<CreateScheduleProxyEndpoint> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/proxy/schedules");
        Policies("schedules:write");
        Summary(s =>
        {
            s.Summary = "Create schedule (proxy)";
            s.Description = "Proxies the request to SchedulerServer to create a new schedule.";
            s.ExampleRequest = new ProxyCreateScheduleRequest
            {
                Name = "DailySync",
                PipelineName = "DataArchiveCopy",
                SchedulerType = "Cron",
                CronExpression = "0 2 * * *",
                IsEnabled = true
            };
        });
        Tags("Schedules");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ProxyCreateScheduleRequest req, CancellationToken ct)
    {
        ProxyLog.ProxyRequest(_logger, "POST", "Scheduler", "schedules");

        var request = new CreateScheduleClientRequest
        {
            Name = req.Name,
            PipelineName = req.PipelineName,
            SchedulerType = req.SchedulerType,
            CronExpression = req.CronExpression,
            IntervalSeconds = req.IntervalSeconds,
            IsEnabled = req.IsEnabled
        };

        var result = await _client.CreateSchedule(request, ct);

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
