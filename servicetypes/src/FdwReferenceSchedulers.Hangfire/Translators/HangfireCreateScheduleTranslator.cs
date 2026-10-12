using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Scheduling.Abstractions;
using Hangfire;
using Hangfire.Common;
using FdwReferenceSchedulers.Hangfire.Logging;
using FdwReferenceSchedulers.Hangfire.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceSchedulers.Hangfire.Translators;

/// <summary>Translates <see cref="CreateScheduleCommand"/> into a Hangfire <c>AddOrUpdate</c> call.</summary>
[TypeOption(typeof(HangfireSchedulingTranslators), "Create")]
public sealed class HangfireCreateScheduleTranslator : HangfireSchedulingTranslatorBase
{
    /// <summary>Initializes a new instance of the <see cref="HangfireCreateScheduleTranslator"/> class.</summary>
    public HangfireCreateScheduleTranslator() : base("Create")
    {
    }

    /// <inheritdoc />
    public override Task<IGenericResult<HangfireNativeAction>> Translate(
        ISchedulingCommand command, CancellationToken cancellationToken = default)
    {
        if (command is not CreateScheduleCommand create)
        {
            return Task.FromResult(GenericResult<HangfireNativeAction>.Failure(
                HangfireSchedulingLog.NativeCallFailed(NullLogger.Instance, new InvalidOperationException(
                    $"HangfireCreateScheduleTranslator cannot translate {command.GetType().Name}"), "(unknown)")));
        }

        var schedule = create.Schedule;
        if (schedule.CronExpression is not { Length: > 0 } cron)
        {
            return Task.FromResult(GenericResult<HangfireNativeAction>.Failure(
                HangfireSchedulingLog.CronExpressionMissing(NullLogger.Instance, schedule.ScheduleId)));
        }
        var job = Job.FromExpression<HangfireJobDispatcher>(
            d => d.Execute(schedule.ScheduleId, schedule.ProcessId, create.DataStoreName, create.PathName, create.ScheduleContainerName));
        var options = new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById(schedule.TimeZoneId) };

        HangfireNativeAction action = manager =>
        {
            try
            {
                manager.AddOrUpdate(schedule.ScheduleId, job, cron, options);
                return GenericResult.Success();
            }
            catch (Exception ex)
            {
                return GenericResult.Failure(HangfireSchedulingLog.NativeCallFailed(NullLogger.Instance, ex, schedule.ScheduleId));
            }
        };

        return Task.FromResult(GenericResult<HangfireNativeAction>.Success(action));
    }
}
