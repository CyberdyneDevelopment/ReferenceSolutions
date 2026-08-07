using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Abstractions;
using Fdw.Services.Scheduling.Abstractions;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Reference.Scheduler.Server.Logging;
using Reference.Scheduler.Server.Models;

namespace Reference.Scheduler.Server.Endpoints;

[ExcludeFromCodeCoverage]
public sealed class UpdateScheduleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? PipelineName { get; set; }
    public string? SchedulerType { get; set; }
    public string? CronExpression { get; set; }
    public int? IntervalSeconds { get; set; }
    public string? TimeZoneId { get; set; }
    public bool? IsEnabled { get; set; }
}

[ExcludeFromCodeCoverage]
public sealed class UpdateScheduleResponse
{
    public bool Success { get; set; }
    public string Name { get; set; } = string.Empty;
}

[ExcludeFromCodeCoverage]
public sealed class UpdateScheduleEndpoint : Endpoint<UpdateScheduleRequest, UpdateScheduleResponse>
{
    private readonly IFrameworkSchedulingService _schedulingService;
    // Why: IServiceConfigurationProvider<ScheduleConfiguration> replaces IOptionsMonitor<List<SchedulerConfiguration>>
    // + raw DataGateway queries. The provider already merges ctrl + cfg schedule data via the dual-source pattern.
    private readonly IServiceConfigurationProvider<ScheduleConfiguration> _scheduleProvider;
    private readonly ILogger<UpdateScheduleEndpoint> _logger;

    public UpdateScheduleEndpoint(
        IFrameworkSchedulingService schedulingService,
        IServiceConfigurationProvider<ScheduleConfiguration> scheduleProvider,
        ILogger<UpdateScheduleEndpoint> logger)
    {
        _schedulingService = schedulingService;
        _scheduleProvider = scheduleProvider;
        _logger = logger;
    }

    public override void Configure()
    {
        Put("schedules/{Name}");
        // Why: Scheduler endpoints are anonymous — authentication is enforced at the service boundary (bearer token from callers).
        Policies("schedules:write");
        Summary(s =>
        {
            s.Summary = "Update a schedule";
            s.Description = "Updates an existing schedule's timing and configuration.";
        });
    }

    public override async Task HandleAsync(UpdateScheduleRequest req, CancellationToken ct)
    {
        SchedulerServerLog.UpdateScheduleRequestReceived(_logger, req.Name);

        // Get existing schedule from the configuration provider
        var existingResult = await _scheduleProvider.Get(req.Name, ct).ConfigureAwait(false);
        if (!existingResult.IsSuccess || existingResult.Value is null)
        {
            SchedulerServerLog.ScheduleNotFound(_logger, req.Name);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var existing = existingResult.Value;

        // Merge request with existing, then update via FDW service
        var merged = new ScheduleDto
        {
            ScheduleId = req.Name,
            ScheduleName = req.Name,
            ProcessId = req.PipelineName ?? existing.PipelineName,
            CronExpression = req.CronExpression ?? existing.CronExpression ?? string.Empty,
            TimeZoneId = req.TimeZoneId ?? existing.TimeZoneId ?? string.Empty,
            IsActive = req.IsEnabled ?? existing.IsEnabled,
            NextExecution = existing.NextRunTime?.UtcDateTime
        };

        var result = await _schedulingService.UpdateSchedule(merged, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            AddError(SchedulerServerLog.GetError(result, _logger));
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(new UpdateScheduleResponse
        {
            Success = true,
            Name = req.Name
        }, ct).ConfigureAwait(false);
    }
}
