using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Extensions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Scheduling.Abstractions;
using Fdw.Services.Scheduling.Execution;
using Microsoft.Extensions.Logging;
using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceSchedulers.Hangfire.Logging;
using ProcessStateKinds = Fdw.Services.Execution.Abstractions.OptionTypes.States;

namespace FdwReferenceSchedulers.Hangfire.Services;

/// <summary>
/// Bridges Hangfire's job invocation to <see cref="IGenericScheduledExecutionHandler"/>. Hangfire's
/// job storage serializes a reference to <see cref="Execute"/> (instance method expression) plus its
/// arguments at registration time, and resolves this class per fire via its own job activator (DI).
/// Also records the fire in the scheduler's own execution-history container.
/// </summary>
/// <remarks>
/// The target (dataStoreName/pathName/scheduleContainerName) arrives as method arguments rather
/// than injected configuration: Hangfire serializes the argument values with the recurring job, so
/// they survive to fire-time the same way Quartz's JobDataMap entries do. There is no single
/// <see cref="ISchedulerImplementationConfiguration"/> DI could hand a job resolved per fire — the
/// command that scheduled it already carried the target.
/// </remarks>
public sealed class HangfireJobDispatcher
{
    private const string HistoryContainerSuffix = "History";

    // Why the provider and not IDataGateway: FDW never registers IDataGateway. The data-gateway option
    // registers IDataGatewayFactory and IDataGatewayProvider only, because Create() builds a fresh gateway
    // per ask and a registration would capture one across a scope boundary. A job that asks DI for the
    // gateway fails when it is built -- "Unable to resolve service for type IDataGateway" -- and the
    // schedule never fires (FDW-1055). Services take providers; only providers take gateways.
    private const string MainDataGatewayName = "Main";

    private readonly IGenericScheduledExecutionHandler _executionHandler;
    private readonly IDataGatewayProvider _dataGateways;
    private readonly IAuthenticationContextAccessor _authenticationContextAccessor;
    private readonly ILogger<HangfireJobDispatcher> _logger;

    private IDataGateway Gateway => _dataGateways.ByName(MainDataGatewayName);

    /// <summary>Initializes a new instance of the <see cref="HangfireJobDispatcher"/> class.</summary>
    public HangfireJobDispatcher(
        IGenericScheduledExecutionHandler executionHandler,
        IDataGatewayProvider dataGateways,
        IAuthenticationContextAccessor authenticationContextAccessor,
        ILogger<HangfireJobDispatcher>? logger = null)
    {
        _executionHandler = executionHandler ?? throw new ArgumentNullException(nameof(executionHandler));
        _dataGateways = dataGateways ?? throw new ArgumentNullException(nameof(dataGateways));
        _authenticationContextAccessor = authenticationContextAccessor ?? throw new ArgumentNullException(nameof(authenticationContextAccessor));
        _logger = logger ?? NullLogger<HangfireJobDispatcher>.Instance;
    }

    /// <summary>Invoked by Hangfire when a recurring job fires.</summary>
    /// <param name="scheduleId">The identifier of the schedule that owns this job.</param>
    /// <param name="processId">The identifier of the process to execute.</param>
    /// <param name="dataStoreName">The connection to record execution history in.</param>
    /// <param name="pathName">The schema to record execution history in.</param>
    /// <param name="scheduleContainerName">The schedule container, whose history container this appends to.</param>
    public async Task Execute(
        string scheduleId, string processId, string dataStoreName, string pathName, string scheduleContainerName)
    {
        var startedAt = DateTimeOffset.UtcNow;

        // Why elevated: a background fire has no ClaimsPrincipal, so without this the connection
        // resolves to the deny-everywhere principal and reaches only fn_TenantFilter's shared-row
        // branch — reads come back partial and silent, and an insert whose row is not visible is
        // rejected outright by SchedPolicy's AFTER INSERT block predicate. System elevation is the
        // decision recorded in FDW-767: deliberate, temporary, and it means this run is not
        // tenant-scoped. Revisit by giving the run a real Guid principal.
        using var elevation = new SystemAuthenticationContextScope(_authenticationContextAccessor);

        var result = await _executionHandler.ExecuteScheduledProcess(
            processId, scheduleId, startedAt.UtcDateTime, startedAt.UtcDateTime, metadata: null).ConfigureAwait(false);

        // Why the error is set per path (FDW-902): the row's error column holds why a run failed, and
        // nothing on a real success -- a success's CurrentMessage is not an error, and a success with no
        // process result is recorded as Failed, so its row names that reason.
        string? errorMessage = null;
        if (!result.IsSuccess)
        {
            errorMessage = result.CurrentMessage;
            HangfireSchedulingLog.JobExecutionFailed(_logger, scheduleId, processId, result.CurrentMessage);
        }
        else if (result.Value is null)
        {
            errorMessage = HangfireSchedulingLog.HandlerReturnedNoResult(_logger, scheduleId, processId).Message;
        }

        // Why the handler's own state (FDW-902): a success means the handler did its part, not that
        // the process finished -- EtlDispatch returns Pending once the ETL server has queued the run.
        // Recording "Succeeded" there claimed an outcome nobody had seen, and was not a ProcessStates
        // name, so reading the history back through the collection resolved to nothing.
        var status = result.IsSuccess && result.Value is { } processResult
            ? processResult.FinalState.Name
            : nameof(ProcessStateKinds.Failed);

        await RecordExecution(
            scheduleId, processId, dataStoreName, pathName, scheduleContainerName,
            startedAt, status, errorMessage).ConfigureAwait(false);
    }

    private async Task RecordExecution(
        string scheduleId, string processId, string dataStoreName, string pathName, string scheduleContainerName,
        DateTimeOffset startedAt, string status, string? errorMessage)
    {
        var completedAt = DateTimeOffset.UtcNow;
        var record = new ScheduleExecutionRecord
        {
            Name = $"{scheduleId}_{completedAt:yyyyMMddHHmmss}",
            ScheduleName = scheduleId,
            PipelineName = processId,
            TriggerType = "Scheduled",
            Status = status,
            TriggeredAt = startedAt,
            ScheduledFireTime = startedAt,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            DurationMs = (long)(completedAt - startedAt).TotalMilliseconds,
            ErrorMessage = errorMessage,
            ExecutedBy = Environment.MachineName
        };

        var command = Insert.Into<ScheduleExecutionRecord>(scheduleContainerName + HistoryContainerSuffix)
            .DataStore(dataStoreName)
            .Path(pathName)
            .Value(record);

        var insertResult = await Gateway.Execute<int>(command, CancellationToken.None).ConfigureAwait(false);
        if (!insertResult.IsSuccess)
        {
            HangfireSchedulingLog.NativeCallFailed(_logger,
                new InvalidOperationException(insertResult.CurrentMessage ?? "Unknown error"), scheduleId);
        }
    }
}
