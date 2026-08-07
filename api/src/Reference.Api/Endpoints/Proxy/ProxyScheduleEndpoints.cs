using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Scheduling.Clients.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Reference.Api.Constants;
using Reference.Api.Logging;

namespace Reference.Api.Endpoints.Proxy;

// ═══════════════════════════════════════════════════════════════════════════
// Proxy Schedule Request/Response DTOs
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Request for creating a schedule via proxy.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ProxyCreateScheduleRequest
{
    /// <summary>
    /// Gets or sets the schedule name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pipeline name.
    /// </summary>
    public string PipelineName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the scheduler type (Cron, Interval, Manual).
    /// </summary>
    public string SchedulerType { get; set; } = "Cron";

    /// <summary>
    /// Gets or sets the cron expression.
    /// </summary>
    public string? CronExpression { get; set; }

    /// <summary>
    /// Gets or sets the interval in seconds.
    /// </summary>
    public int? IntervalSeconds { get; set; }

    /// <summary>
    /// Gets or sets whether the schedule is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}

// ═══════════════════════════════════════════════════════════════════════════
// Proxy Schedule Endpoints
// ═══════════════════════════════════════════════════════════════════════════

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
            AddError(Constants.ErrorMessages.FailedToProxyScheduler);
            await Send.ErrorsAsync(502, ct);
            return;
        }

        ProxyLog.ProxyResponse(_logger, "Scheduler", 200);
        await Send.OkAsync(result.Value!, ct);
    }
}

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
            AddError(Constants.ErrorMessages.FailedToProxyScheduler);
            await Send.ErrorsAsync(502, ct);
            return;
        }

        ProxyLog.ProxyResponse(_logger, "Scheduler", 200);
        await Send.OkAsync(result.Value!, ct);
    }
}
