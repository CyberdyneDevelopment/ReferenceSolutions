using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Scheduling.Abstractions;
using Microsoft.Extensions.Logging;
using Reference.Scheduler.Server.Logging;
using Reference.Scheduler.Server.Models;

namespace Reference.Scheduler.Server.Endpoints;

[ExcludeFromCodeCoverage]
public sealed class CreateScheduleRequest
{
    public string Name { get; set; } = string.Empty;
    public string PipelineName { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
}

[ExcludeFromCodeCoverage]
public sealed class CreateScheduleResponse
{
    public string ScheduleId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

[ExcludeFromCodeCoverage]
public sealed class CreateScheduleEndpoint : Endpoint<CreateScheduleRequest, CreateScheduleResponse>
{
    private readonly IFrameworkSchedulingService _schedulingService;
    private readonly ILogger<CreateScheduleEndpoint> _logger;

    public CreateScheduleEndpoint(IFrameworkSchedulingService schedulingService, ILogger<CreateScheduleEndpoint> logger)
    {
        _schedulingService = schedulingService;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("schedules");
        // Why: Scheduler endpoints are anonymous — authentication is enforced at the service boundary (bearer token from callers).
        Policies("schedules:write");
        Summary(s =>
        {
            s.Summary = "Create a new schedule";
            s.Description = "Creates a new pipeline schedule with the specified cron expression.";
            s.ExampleRequest = new CreateScheduleRequest
            {
                Name = "DailySync",
                PipelineName = "NflDataPipeline",
                CronExpression = "0 0 * * *"
            };
        });
    }

    public override async Task HandleAsync(CreateScheduleRequest req, CancellationToken ct)
    {
        SchedulerServerLog.CreateScheduleRequestReceived(_logger, req.Name);

        var schedule = new ScheduleDto
        {
            ScheduleId = req.Name,
            ScheduleName = req.Name,
            ProcessId = req.PipelineName,
            CronExpression = req.CronExpression,
            IsActive = true,
            TimeZoneId = "UTC"
        };

        var result = await _schedulingService.CreateSchedule(schedule, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            AddError(SchedulerServerLog.GetError(result, _logger));
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        await Send.ResponseAsync(new CreateScheduleResponse
        {
            ScheduleId = req.Name,
            Name = req.Name
        }, 201, ct).ConfigureAwait(false);
    }
}
