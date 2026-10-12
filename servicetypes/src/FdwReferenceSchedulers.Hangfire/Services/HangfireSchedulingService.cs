using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Execution.Abstractions.OptionTypes;
using Fdw.Services.Scheduling.Abstractions;
using Fdw.Services.Scheduling.Execution;
using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceSchedulers.Hangfire.Logging;
using FdwReferenceSchedulers.Hangfire.Translators;
using TriggerKinds = Fdw.Services.Scheduling.Abstractions.OptionTypes.TriggerTypeImplementations;

namespace FdwReferenceSchedulers.Hangfire.Services;

/// <summary>
/// Hangfire-backed implementation of <see cref="IFrameworkSchedulingService"/>. Each write operation
/// builds the shared <see cref="ISchedulingCommand"/> for that operation, translates it through
/// <see cref="HangfireSchedulingTranslators"/>, then executes the resulting native call against the
/// live <see cref="IRecurringJobManager"/>.
/// </summary>
/// <remarks>
/// Execution history reads/writes go through
/// <see cref="ISchedulerImplementationConfiguration.DataStoreName"/>/<c>PathName</c>/
/// <c>ScheduleContainerName + "History"</c> — the target this scheduler's own configuration assigns,
/// the same values <see cref="HangfireJobDispatcher"/> writes to per fire.
/// <para>
/// <see cref="ResumeSchedule"/> fails by design: Hangfire has no paused state to resume from, so
/// re-enabling a schedule goes through <see cref="CreateSchedule"/> with the full definition from
/// configuration — which is exactly what <see cref="HangfireReconciliationService"/> does when a
/// row flips back to enabled. See <see cref="HangfireResumeScheduleTranslator"/>.
/// </para>
/// </remarks>
public sealed class HangfireSchedulingService : IFrameworkSchedulingService
{
    private readonly ILogger<HangfireSchedulingService> _logger;
    private readonly IRecurringJobManager _manager;
    private readonly IDataGateway _dataGateway;
    private readonly ISchedulerImplementationConfiguration _configuration;

    /// <summary>Initializes a new instance of the <see cref="HangfireSchedulingService"/> class.</summary>
    public HangfireSchedulingService(
        ILogger<HangfireSchedulingService> logger,
        IRecurringJobManager manager,
        IDataGateway dataGateway,
        ISchedulerImplementationConfiguration configuration)
    {
        _logger = logger ?? NullLogger<HangfireSchedulingService>.Instance;
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        _dataGateway = dataGateway ?? throw new ArgumentNullException(nameof(dataGateway));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <inheritdoc />
    public IGenericScheduler Scheduler =>
        throw new NotSupportedException(
            "Direct scheduler access is not available in HangfireSchedulingService. Use the service methods for schedule management.");

    /// <inheritdoc />
    public string Id => _configuration.Id.ToString("N");

    /// <inheritdoc />
    public string Name => _configuration.Name;

    /// <inheritdoc />
    public string ServiceType => "Scheduler";

    /// <inheritdoc />
    public bool IsAvailable => true;

    /// <inheritdoc />
    public Task<IGenericResult<T>> Execute<T>(IGenericCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult(GenericResult<T>.Failure(
            HangfireSchedulingLog.NativeCallFailed(_logger,
                new NotSupportedException("IGenericCommand execution is not supported; use the typed service methods."),
                "(unknown)")));

    /// <inheritdoc />
    public Task<IGenericResult> Execute(IGenericCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult((IGenericResult)GenericResult.Failure(
            HangfireSchedulingLog.NativeCallFailed(_logger,
                new NotSupportedException("IGenericCommand execution is not supported; use the typed service methods."),
                "(unknown)")));

    /// <inheritdoc />
    public Task<IGenericResult> CreateSchedule(IGenericSchedule schedule, CancellationToken cancellationToken = default)
        => Execute(
            new CreateScheduleCommand(schedule, _configuration.DataStoreName, _configuration.PathName, _configuration.ScheduleContainerName),
            HangfireSchedulingTranslators.Create, schedule.ScheduleId);

    /// <inheritdoc />
    public Task<IGenericResult> UpdateSchedule(IGenericSchedule schedule, CancellationToken cancellationToken = default)
        => Execute(
            new UpdateScheduleCommand(schedule, _configuration.DataStoreName, _configuration.PathName, _configuration.ScheduleContainerName),
            HangfireSchedulingTranslators.Update, schedule.ScheduleId);

    /// <inheritdoc />
    public Task<IGenericResult> DeleteSchedule(string scheduleId, CancellationToken cancellationToken = default)
        => Execute(
            new DeleteScheduleCommand(scheduleId, _configuration.DataStoreName, _configuration.PathName, _configuration.ScheduleContainerName),
            HangfireSchedulingTranslators.Delete, scheduleId);

    /// <inheritdoc />
    public Task<IGenericResult> PauseSchedule(string scheduleId, CancellationToken cancellationToken = default)
        => Execute(
            new PauseScheduleCommand(scheduleId, _configuration.DataStoreName, _configuration.PathName, _configuration.ScheduleContainerName),
            HangfireSchedulingTranslators.Pause, scheduleId);

    /// <inheritdoc />
    public Task<IGenericResult> ResumeSchedule(string scheduleId, CancellationToken cancellationToken = default)
        => Execute(
            new ResumeScheduleCommand(scheduleId, _configuration.DataStoreName, _configuration.PathName, _configuration.ScheduleContainerName),
            HangfireSchedulingTranslators.Resume, scheduleId);

    /// <inheritdoc />
    public Task<IGenericResult> TriggerSchedule(string scheduleId, CancellationToken cancellationToken = default)
        => Execute(
            new TriggerScheduleCommand(scheduleId, _configuration.DataStoreName, _configuration.PathName, _configuration.ScheduleContainerName),
            HangfireSchedulingTranslators.Trigger, scheduleId);

    /// <inheritdoc />
    public Task<IGenericResult<IGenericSchedule?>> GetSchedule(string scheduleId, CancellationToken cancellationToken = default)
    {
        var dto = GetRecurringJobs().FirstOrDefault(j => string.Equals(j.Id, scheduleId, StringComparison.Ordinal));
        return Task.FromResult(GenericResult<IGenericSchedule?>.Success(dto is null ? null : ToGenericSchedule(dto)));
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyCollection<IGenericSchedule>>> GetSchedules(
        bool includeInactive = true, CancellationToken cancellationToken = default)
    {
        var schedules = GetRecurringJobs()
            .Where(j => includeInactive || !j.Removed)
            .Select(ToGenericSchedule)
            .Cast<IGenericSchedule>()
            .ToList();

        return Task.FromResult(GenericResult<IReadOnlyCollection<IGenericSchedule>>.Success(schedules));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyCollection<IGenericScheduleExecutionHistory>>> GetScheduleHistory(
        string scheduleId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var query = Query.From<ScheduleExecutionRecord>(
                _configuration.DataStoreName,
                _configuration.PathName,
                _configuration.ScheduleContainerName + "History")
            .Where("ScheduleName", scheduleId);

        if (startDate.HasValue)
        {
            query = query.Where(r => r.TriggeredAt).GreaterThanOrEqual(new DateTimeOffset(startDate.Value, TimeSpan.Zero));
        }

        if (endDate.HasValue)
        {
            query = query.Where(r => r.TriggeredAt).LessThanOrEqual(new DateTimeOffset(endDate.Value, TimeSpan.Zero));
        }

        var command = query.OrderBy("TriggeredAt").Build();
        var result = await _dataGateway.Execute<IEnumerable<ScheduleExecutionRecord>>(command, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
            return result.ToNewResult<IReadOnlyCollection<IGenericScheduleExecutionHistory>>();

        IReadOnlyCollection<IGenericScheduleExecutionHistory> history = result.Value!
            .Select(ToExecutionHistory)
            .ToList();

        return GenericResult<IReadOnlyCollection<IGenericScheduleExecutionHistory>>.Success(history);
    }

    private static HangfireExecutionHistoryRecord ToExecutionHistory(ScheduleExecutionRecord record)
        => new HangfireExecutionHistoryRecord(
            executionId: record.Id.ToString("N"),
            scheduleId: record.ScheduleName,
            triggeredAt: record.TriggeredAt.UtcDateTime,
            startedAt: record.StartedAt?.UtcDateTime,
            completedAt: record.CompletedAt?.UtcDateTime,
            duration: record.DurationMs.HasValue ? TimeSpan.FromMilliseconds(record.DurationMs.Value) : null,
            wasTriggeredManually: string.Equals(record.TriggerType, "Manual", StringComparison.Ordinal),
            status: ProcessStates.ByName(record.Status),
            errorMessage: record.ErrorMessage);

    private async Task<IGenericResult> Execute(
        ISchedulingCommand command, ISchedulingCommandTranslator<HangfireNativeAction> translator, string scheduleId)
    {
        HangfireSchedulingLog.TranslatingCommand(_logger, command.CommandType, scheduleId);

        var translation = await translator.Translate(command).ConfigureAwait(false);
        if (!translation.IsSuccess || translation.Value is null)
            return translation;

        return translation.Value(_manager);
    }

    // Materialized deliberately: the storage connection is disposed when this returns, so handing
    // back a lazy sequence would have the caller enumerate it after disposal.
    private static List<RecurringJobDto> GetRecurringJobs()
    {
        using var connection = JobStorage.Current.GetConnection();
        return [.. connection.GetRecurringJobs()];
    }

    private static HangfireScheduleRecord ToGenericSchedule(RecurringJobDto dto)
    {
        var processId = dto.Job?.Args?.Count > 0 ? dto.Job.Args[1]?.ToString() ?? string.Empty : string.Empty;
        return new HangfireScheduleRecord(
            scheduleId: dto.Id,
            scheduleName: dto.Id,
            processId: processId,
            cronExpression: dto.Cron,
            isActive: !dto.Removed,
            // Why UTC for a null zone: that is Hangfire's own meaning of a recurring job with no
            // TimeZoneId, not a value chosen here.
            timeZoneId: dto.TimeZoneId ?? TimeZoneInfo.Utc.Id,
            nextExecution: dto.NextExecution);
    }

    private sealed class HangfireScheduleRecord : IGenericSchedule
    {
        public HangfireScheduleRecord(
            string scheduleId, string scheduleName, string processId, string? cronExpression,
            bool isActive, string timeZoneId, DateTime? nextExecution)
        {
            ScheduleId = scheduleId;
            ScheduleName = scheduleName;
            ProcessId = processId;
            CronExpression = cronExpression;
            IsActive = isActive;
            TimeZoneId = timeZoneId;
            NextExecution = nextExecution;
        }

        public string ScheduleId { get; }
        public string ScheduleName { get; }
        public string ProcessId { get; }
        // Why Cron: a Hangfire recurring job is defined by a cron expression; it has no other kind.
        public string TriggerType => nameof(TriggerKinds.Cron);
        public string? CronExpression { get; }
        public int? IntervalSeconds => null;
        public DateTime? ExecuteAtUtc => null;
        public DateTime? NextExecution { get; }
        public bool IsActive { get; }
        public string TimeZoneId { get; }
        public IReadOnlyDictionary<string, object>? Metadata => null;
    }

    private sealed class HangfireExecutionHistoryRecord : IGenericScheduleExecutionHistory
    {
        public HangfireExecutionHistoryRecord(
            string executionId, string scheduleId, DateTime triggeredAt, DateTime? startedAt,
            DateTime? completedAt, TimeSpan? duration, bool wasTriggeredManually,
            IProcessState status, string? errorMessage)
        {
            ExecutionId = executionId;
            ScheduleId = scheduleId;
            TriggeredAt = triggeredAt;
            StartedAt = startedAt;
            CompletedAt = completedAt;
            Duration = duration;
            WasTriggeredManually = wasTriggeredManually;
            Status = status;
            ErrorMessage = errorMessage;
        }

        public string ExecutionId { get; }
        public string ScheduleId { get; }
        public DateTime TriggeredAt { get; }
        public DateTime? StartedAt { get; }
        public DateTime? CompletedAt { get; }
        public TimeSpan? Duration { get; }
        public bool WasTriggeredManually { get; }
        public IProcessState Status { get; }
        public string? ErrorMessage { get; }
        public IReadOnlyDictionary<string, object>? Metadata => null;
    }
}
