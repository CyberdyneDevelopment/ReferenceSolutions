using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Scheduling;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Abstractions.Models;
using Fdw.Services.Scheduling.Abstractions.OptionTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Reference.Scheduler.Server.Logging;
using LocalConfig = Reference.Scheduler.Server.Configuration.SchedulerConfiguration;
using Reference.Scheduler.Server.Queries;

namespace Reference.Scheduler.Server.Services;

/// <summary>
/// Background service that continuously evaluates schedules and dispatches jobs.
/// Uses FDW TriggerTypes for schedule evaluation instead of local JobScheduler implementations.
/// </summary>
public sealed class SchedulerBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<SchedulerBackgroundService> _logger;
    private readonly LocalConfig _options;
    private readonly ConcurrentDictionary<string, bool> _inFlight = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulerBackgroundService"/> class.
    /// </summary>
    /// <param name="serviceScopeFactory">Factory for creating service scopes.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="options">Scheduler configuration options.</param>
    public SchedulerBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<SchedulerBackgroundService> logger,
        IOptions<LocalConfig> options)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger ?? NullLogger<SchedulerBackgroundService>.Instance;
        _options = options.Value;
    }

    /// <summary>
    /// Executes the background service, continuously evaluating schedules.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token for stopping the service.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = _options.EvaluationIntervalSeconds;
        SchedulerServerLog.SchedulerStarted(_logger, intervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EvaluateSchedules(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                SchedulerServerLog.EvaluationLoopError(_logger, ex);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        SchedulerServerLog.SchedulerStopped(_logger);
    }

#pragma warning disable MA0051 // Sequential schedule evaluation with dispatch and optimistic update
    private async Task EvaluateSchedules(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        // Why: IDataGateway is scoped — resolved per evaluation cycle to write timestamp updates
        // directly, bypassing IFrameworkSchedulingService.Execute which no longer accepts
        // IDataCommand in FDW 1.5.0 (the generic Execute route was removed; use typed service
        // methods for schedule CRUD and IDataGateway directly for raw update writes).
        var dataGateway = scope.ServiceProvider.GetRequiredService<IDataGateway>();
        var dispatchService = scope.ServiceProvider.GetRequiredService<IEtlDispatchService>();

        // Why: IServiceConfigurationProvider<ScheduleConfiguration> replaces IOptionsMonitor<List<SchedulerConfiguration>>
        // + raw DataGateway queries for reading schedules. The provider merges ctrl + cfg schedule data.
        var scheduleProvider = scope.ServiceProvider.GetRequiredService<IServiceConfigurationProvider<ScheduleConfiguration>>();

        // Why: SchedulerConfiguration is still needed for write operations (UpdateTimestamps)
        // because the DataGateway write commands need DataStoreName/PathName/ScheduleContainerName.
        // Read through IServiceConfigurationProvider — the IOptionsMonitor is empty in production
        // because SchedulerConfiguration is loaded from ConfigurationDb via ConfigurationGateway.
        var schedulerProvider = scope.ServiceProvider.GetRequiredService<IServiceConfigurationProvider<SchedulerConfiguration>>();
        var schedulerResult = await schedulerProvider.Get(cancellationToken).ConfigureAwait(false);
        if (!schedulerResult.IsSuccess || schedulerResult.Value is not { Count: > 0 } schedulers)
        {
            SchedulerServerLog.EvaluationStarted(_logger, 0);
            return;
        }
        var fdwConfig = schedulers[0];

        var allResult = await scheduleProvider.Get(cancellationToken).ConfigureAwait(false);
        var records = (allResult.IsSuccess && allResult.Value is not null)
            ? allResult.Value.ToList()
            : new List<ScheduleConfiguration>();

        var now = DateTimeOffset.UtcNow;
        var jobsDispatched = 0;

        SchedulerServerLog.EvaluationStarted(_logger, records.Count);

        foreach (var record in records)
        {
            if (!record.IsEnabled)
                continue;

            SchedulerServerLog.ScheduleEvaluating(_logger, record.Name);

            if (string.IsNullOrEmpty(record.ServiceOptionType))
            {
                SchedulerServerLog.EvaluatorNotFound(_logger, record.Name);
                continue;
            }

            // Map reference-scheduler type names to FDW TriggerTypes
            var triggerTypeName = MapTriggerTypeName(record.ServiceOptionType);
            var triggerType = TriggerTypes.ByName(triggerTypeName);
            if (triggerType == TriggerTypes.NotFound)
            {
                SchedulerServerLog.EvaluatorNotFound(_logger, record.ServiceOptionType);
                continue;
            }

            // Build an IGenericTrigger from the ScheduleConfiguration for FDW's API
            var trigger = BuildTrigger(record);
            var lastRun = record.LastRunTime?.UtcDateTime;

            if (triggerType.IsDue(trigger, lastRun, now))
            {
                // Skip schedules already in-flight to prevent duplicate dispatches
                if (!_inFlight.TryAdd(record.Name, true))
                {
                    SchedulerServerLog.ScheduleAlreadyInFlight(_logger, record.Name);
                    continue;
                }

                SchedulerServerLog.ScheduleIsDue(_logger, record.Name, record.PipelineName);

                // Calculate next run time using FDW's TriggerType
                var nextExecution = triggerType.CalculateNextExecution(trigger, now.UtcDateTime);
                DateTimeOffset? nextRun = nextExecution.HasValue
                    ? new DateTimeOffset(nextExecution.Value, TimeSpan.Zero)
                    : null;

                // Optimistic update: set LastRunTime + LastRunStatus BEFORE dispatch to prevent
                // duplicate dispatches on concurrent evaluations. Status is written optimistically as
                // Succeeded and flipped to Failed on the revert path below — the same optimistic shape
                // already used for LastRunTime.
                var preUpdateResult = await dataGateway.Execute<int>(
                        ScheduleQueries.UpdateTimestamps(fdwConfig, record.Name, now, nextRun, ScheduleStatuses.Succeeded),
                        cancellationToken)
                    .ConfigureAwait(false);

                if (!preUpdateResult.IsSuccess)
                {
                    SchedulerServerLog.ScheduleUpdateFailed(_logger,
                        new InvalidOperationException(SchedulerServerLog.GetError(preUpdateResult, _logger)),
                        record.Name);
                    _inFlight.TryRemove(record.Name, out _);
                    continue;
                }

                var dispatchResult = await dispatchService.Dispatch(
                    record.Name,
                    record.PipelineName,
                    "Scheduled",
                    record.TenantId,
                    cancellationToken).ConfigureAwait(false);

                if (dispatchResult.IsSuccess)
                {
                    jobsDispatched++;
                }
                else
                {
                    // Revert LastRunTime + record the Failed status on dispatch failure so the schedule
                    // will retry next evaluation and the failure is visible in LastRunStatus.
                    var revertResult = await dataGateway.Execute<int>(
                            ScheduleQueries.UpdateTimestamps(fdwConfig, record.Name, record.LastRunTime, nextRun, ScheduleStatuses.Failed),
                            cancellationToken)
                        .ConfigureAwait(false);

                    if (!revertResult.IsSuccess)
                    {
                        SchedulerServerLog.ScheduleUpdateFailed(_logger,
                            new InvalidOperationException(SchedulerServerLog.GetError(revertResult, _logger)),
                            record.Name);
                    }
                }

                _inFlight.TryRemove(record.Name, out _);
            }
            else
            {
                SchedulerServerLog.ScheduleNotDue(_logger, record.Name);
            }
        }

        SchedulerServerLog.EvaluationCompleted(_logger, records.Count, jobsDispatched);
    }
#pragma warning restore MA0051

    /// <summary>
    /// Maps reference-scheduler ServiceOptionType names to FDW TriggerType names.
    /// </summary>
    private static string MapTriggerTypeName(string serviceOptionType)
    {
        // "OneTime" in reference-scheduler maps to "Once" in FDW TriggerTypes
        if (string.Equals(serviceOptionType, "OneTime", StringComparison.OrdinalIgnoreCase))
            return "Once";

        return serviceOptionType;
    }

    /// <summary>
    /// Builds a Trigger from a <see cref="ScheduleConfiguration"/> for use with FDW's TriggerType API.
    /// </summary>
    private static Trigger BuildTrigger(ScheduleConfiguration schedule)
    {
        var triggerTypeName = MapTriggerTypeName(schedule.ServiceOptionType ?? string.Empty);

        return triggerTypeName switch
        {
            "Cron" => Trigger.CreateCron(
                name: schedule.Name,
                cronExpression: schedule.CronExpression ?? string.Empty,
                timeZoneId: schedule.TimeZoneId ?? string.Empty),
            "Interval" => Trigger.CreateInterval(
                name: schedule.Name,
                intervalMinutes: Math.Max(1, (schedule.IntervalSeconds ?? 60) / 60)),
            "Once" => Trigger.CreateOnce(
                name: schedule.Name,
                executeAtUtc: schedule.NextRunTime?.UtcDateTime ?? DateTime.UtcNow),
            _ => Trigger.CreateManual(name: schedule.Name)
        };
    }
}
