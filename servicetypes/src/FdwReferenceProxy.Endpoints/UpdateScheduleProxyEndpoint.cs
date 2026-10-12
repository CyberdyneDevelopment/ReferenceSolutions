using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
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
/// Proxy endpoint to update a schedule on SchedulerServer.
/// PATCH /api/v1/schedules/{name} -> SchedulerServer PATCH /api/v1/schedules/{name}
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateScheduleProxyEndpoint : Endpoint<ProxyUpdateScheduleRequest>
{
    private readonly IScheduleClient _client;
    private readonly IPipelineClient _pipelines;
    private readonly ILogger<UpdateScheduleProxyEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="UpdateScheduleProxyEndpoint"/> class.</summary>
    /// <param name="client">The schedule client.</param>
    /// <param name="pipelines">The pipeline client, carrying the caller's identity.</param>
    /// <param name="logger">The logger.</param>
    public UpdateScheduleProxyEndpoint(IScheduleClient client, IPipelineClient pipelines, ILogger<UpdateScheduleProxyEndpoint> logger)
    {
        _client = client;
        _pipelines = pipelines;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        // Why PATCH and not PUT: an update states only what it changes, which is what the
        // nullable request properties above express. CrudUpdateEndpointBase.UpdateVerb is PATCH
        // for the same reason, and this proxy must present the verb its downstream accepts.
        Patch("/schedules/{name}");
        Policies("schedules:write");
        Summary(s =>
        {
            s.Summary = "Update schedule (proxy)";
            s.Description = "Proxies the request to SchedulerServer to update an existing schedule.";
        });
        Tags("Schedules");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ProxyUpdateScheduleRequest req, CancellationToken ct)
    {
        ProxyLog.ProxyRequest(_logger, "PATCH", "Scheduler", $"schedules/{req.Name}");

        // A zone the server cannot resolve is refused before anything is looked up or sent (FDW-1170).
        var zone = ScheduleTimeZone.Check(req.TimeZoneId, _logger);
        if (zone.IsFailure)
        {
            var (zoneStatus, zoneRefusal) = ResultHttpStatusMapper.Map(zone, HttpContext);
            await Send.ResultAsync(TypedResults.Json(zoneRefusal, statusCode: zoneStatus)).ConfigureAwait(false);
            return;
        }

        // A scheduler type other than the stored one is refused before anything is sent (FDW-1171), as the scheduler refuses it.
        var type = await JudgeType(req, ct).ConfigureAwait(false);
        if (type.IsFailure)
        {
            var (typeStatus, typeRefusal) = ResultHttpStatusMapper.Map(type, HttpContext);
            await Send.ResultAsync(TypedResults.Json(typeRefusal, statusCode: typeStatus)).ConfigureAwait(false);
            return;
        }

        // A changed pipeline is resolved in the caller's view, as on create (FDW-1088); an unchanged one is not sent.
        PipelineDetailResponse? pipeline = null;
        if (req.PipelineId is { } pipelineId)
        {
            var resolved = await SchedulePipeline.Resolve(_pipelines, req.Name, pipelineId, _logger, ct).ConfigureAwait(false);
            if (resolved.IsFailure)
            {
                var (refusedStatus, refusal) = ResultHttpStatusMapper.Map(resolved, HttpContext);
                await Send.ResultAsync(TypedResults.Json(refusal, statusCode: refusedStatus)).ConfigureAwait(false);
                return;
            }

            pipeline = resolved.Value;
        }

        var judged = await JudgeParameters(req, pipeline, ct).ConfigureAwait(false);
        if (judged.IsFailure)
        {
            var (judgedStatus, judgedRefusal) = ResultHttpStatusMapper.Map(judged, HttpContext);
            await Send.ResultAsync(TypedResults.Json(judgedRefusal, statusCode: judgedStatus)).ConfigureAwait(false);
            return;
        }

        var result = await _client.UpdateSchedule(
            req.Name,
            new UpdateScheduleClientRequest
            {
                PipelineId = pipeline?.Id,
                PipelineName = pipeline?.Name,
                SchedulerType = req.SchedulerType,
                CronExpression = req.CronExpression,
                IntervalSeconds = req.IntervalSeconds,
                TimeZoneId = req.TimeZoneId,
                IsEnabled = req.IsEnabled,
                Parameters = req.Parameters,
                RunRoles = req.RunRoles,
            },
            ct).ConfigureAwait(false);

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

    // The type is the implementation the stored schedule is saved under, so an update cannot change it. It is read from the stored
    // schedule only when the request states one; a request that states none is not judged.
    private async Task<IGenericResult> JudgeType(ProxyUpdateScheduleRequest req, CancellationToken ct)
    {
        if (req.SchedulerType is null)
        {
            return GenericResult.Success();
        }

        var stored = await _client.Get(req.Name, ct).ConfigureAwait(false);
        if (stored.IsFailure || stored.Value is not { } current)
        {
            return stored;
        }

        return ScheduleTypeChange.Check(req.SchedulerType, current.SchedulerType, _logger);
    }

    // The values the scheduler will pass on each run are judged against what the pipeline declares, as a trigger's are
    // (contract v2 item 9). They need judging when the request states values or changes the pipeline; the values the
    // request leaves out are the stored ones.
    private async Task<IGenericResult> JudgeParameters(ProxyUpdateScheduleRequest req, PipelineDetailResponse? changedPipeline, CancellationToken ct)
    {
        if (req.Parameters is null && changedPipeline is null)
        {
            return GenericResult.Success();
        }

        var stored = await _client.Get(req.Name, ct).ConfigureAwait(false);
        if (stored.IsFailure || stored.Value is not { } current)
        {
            return stored;
        }

        var pipeline = changedPipeline is null
            ? await SchedulePipeline.Resolve(_pipelines, req.Name, current.PipelineId, _logger, ct).ConfigureAwait(false)
            : GenericResult<PipelineDetailResponse>.Success(changedPipeline);
        return pipeline.IsFailure
            ? pipeline
            : PipelineParameterCheck.Check(pipeline.Value!.Parameters, req.Parameters is null ? current.Parameters : req.Parameters, _logger);
    }
}
