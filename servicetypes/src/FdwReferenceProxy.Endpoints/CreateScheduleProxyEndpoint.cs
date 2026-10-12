using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Scheduling.Abstractions.Models;
using Fdw.Services.Scheduling.Clients.Abstractions;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FdwReferenceProxy.Endpoints.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>
/// Proxy endpoint to create a schedule on SchedulerServer.
/// POST /api/v1/schedules → SchedulerServer POST /api/v1/schedules
/// </summary>
/// <remarks>
/// The request names its pipeline by id. This host resolves it with the caller's own identity -- the pipeline
/// client carries the caller's token to the ETL host -- and relays the id with the name it resolved; the
/// scheduler stores them without looking again (FDW-1088).
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class CreateScheduleProxyEndpoint : Endpoint<ProxyCreateScheduleRequest, CreateScheduleClientResponse>
{
    private readonly IScheduleClient _client;
    private readonly IPipelineClient _pipelines;
    private readonly ILogger<CreateScheduleProxyEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateScheduleProxyEndpoint"/> class.
    /// </summary>
    public CreateScheduleProxyEndpoint(IScheduleClient client, IPipelineClient pipelines, ILogger<CreateScheduleProxyEndpoint> logger)
    {
        _client = client;
        _pipelines = pipelines;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/schedules");
        Policies("schedules:write");
        Summary(s =>
        {
            s.Summary = "Create schedule (proxy)";
            s.Description = "Proxies the request to SchedulerServer to create a new schedule.";
            s.ExampleRequest = new ProxyCreateScheduleRequest
            {
                Name = "DailySync",
                PipelineId = new System.Guid("01a10d18-c1b3-7cf7-b8d0-4598df9f271f"),
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

        // A zone the server cannot resolve is refused before the pipeline is looked up (FDW-1170).
        var zone = ScheduleTimeZone.Check(req.TimeZoneId, _logger);
        if (zone.IsFailure)
        {
            var (zoneStatus, zoneRefusal) = ResultHttpStatusMapper.Map(zone, HttpContext);
            await Send.ResultAsync(TypedResults.Json(zoneRefusal, statusCode: zoneStatus)).ConfigureAwait(false);
            return;
        }

        var pipeline = await SchedulePipeline.Resolve(_pipelines, req.Name, req.PipelineId, _logger, ct).ConfigureAwait(false);
        if (pipeline.IsFailure)
        {
            var (refusedStatus, refusal) = ResultHttpStatusMapper.Map(pipeline, HttpContext);
            await Send.ResultAsync(TypedResults.Json(refusal, statusCode: refusedStatus)).ConfigureAwait(false);
            return;
        }

        // The values the scheduler will pass on each run are judged against what the pipeline declares, as a trigger's
        // are: a pipeline with a required parameter that has no value here cannot be scheduled (contract v2 item 9).
        var judged = PipelineParameterCheck.Check(pipeline.Value!.Parameters, req.Parameters, _logger);
        if (judged.IsFailure)
        {
            var (judgedStatus, judgedRefusal) = ResultHttpStatusMapper.Map(judged, HttpContext);
            await Send.ResultAsync(TypedResults.Json(judgedRefusal, statusCode: judgedStatus)).ConfigureAwait(false);
            return;
        }

        var request = new CreateScheduleClientRequest
        {
            Name = req.Name,
            PipelineId = pipeline.Value!.Id,
            PipelineName = pipeline.Value.Name,
            SchedulerType = req.SchedulerType,
            CronExpression = req.CronExpression,
            IntervalSeconds = req.IntervalSeconds,
            OneTimeDateTime = req.OneTimeDateTime,
            EventName = req.EventName,
            IsEnabled = req.IsEnabled,
            Parameters = req.Parameters,
            RunRoles = req.RunRoles,
        };

        // The client request declares the zone's one default; a zone the caller did not state leaves it, as it always has.
        if (req.TimeZoneId is not null)
        {
            request.TimeZoneId = req.TimeZoneId;
        }

        var result = await _client.CreateSchedule(request, ct);

        if (!result.IsSuccess)
        {
            ProxyLog.ProxyFailed(_logger, "Scheduler", result.CurrentMessage);
            var (status, problem) = ResultHttpStatusMapper.Map(result, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: status)).ConfigureAwait(false);
            return;
        }

        ProxyLog.ProxyResponse(_logger, "Scheduler", 200);
        await Send.OkAsync(result.Value!, ct);
    }
}
