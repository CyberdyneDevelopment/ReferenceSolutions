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

/// <summary>
/// Translates <see cref="UpdateScheduleCommand"/> into a Hangfire <c>AddOrUpdate</c> call. Hangfire's
/// <c>AddOrUpdate</c> is idempotent create-or-replace, so update and create translate identically.
/// </summary>
[TypeOption(typeof(HangfireSchedulingTranslators), "Update")]
public sealed class HangfireUpdateScheduleTranslator : HangfireSchedulingTranslatorBase
{
    /// <summary>Initializes a new instance of the <see cref="HangfireUpdateScheduleTranslator"/> class.</summary>
    public HangfireUpdateScheduleTranslator() : base("Update")
    {
    }

    /// <inheritdoc />
    public override Task<IGenericResult<HangfireNativeAction>> Translate(
        ISchedulingCommand command, CancellationToken cancellationToken = default)
    {
        if (command is not UpdateScheduleCommand update)
        {
            return Task.FromResult(GenericResult<HangfireNativeAction>.Failure(
                HangfireSchedulingLog.NativeCallFailed(NullLogger.Instance, new InvalidOperationException(
                    $"HangfireUpdateScheduleTranslator cannot translate {command.GetType().Name}"), "(unknown)")));
        }

        var schedule = update.Schedule;
        if (schedule.CronExpression is not { Length: > 0 } cron)
        {
            return Task.FromResult(GenericResult<HangfireNativeAction>.Failure(
                HangfireSchedulingLog.CronExpressionMissing(NullLogger.Instance, schedule.ScheduleId)));
        }
        var job = Job.FromExpression<HangfireJobDispatcher>(
            d => d.Execute(schedule.ScheduleId, schedule.ProcessId, update.DataStoreName, update.PathName, update.ScheduleContainerName));
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
