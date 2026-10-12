using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Scheduling.Abstractions;
using Quartz;
using FdwReferenceSchedulers.Quartz.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceSchedulers.Quartz.Translators;

/// <summary>Translates <see cref="UpdateScheduleCommand"/> into a Quartz <c>RescheduleJob</c> call.</summary>
[TypeOption(typeof(QuartzSchedulingTranslators), "Update")]
public sealed class QuartzUpdateScheduleTranslator : QuartzSchedulingTranslatorBase
{
    /// <summary>Initializes a new instance of the <see cref="QuartzUpdateScheduleTranslator"/> class.</summary>
    public QuartzUpdateScheduleTranslator() : base("Update")
    {
    }

    /// <inheritdoc />
    public override Task<IGenericResult<QuartzNativeAction>> Translate(
        ISchedulingCommand command, CancellationToken cancellationToken = default)
    {
        if (command is not UpdateScheduleCommand update)
        {
            return Task.FromResult(GenericResult<QuartzNativeAction>.Failure(
                QuartzSchedulingLog.NativeCallFailed(NullLogger.Instance, new InvalidOperationException(
                    $"QuartzUpdateScheduleTranslator cannot translate {command.GetType().Name}"), "(unknown)")));
        }

        var schedule = update.Schedule;
        var triggerKey = new TriggerKey(schedule.ScheduleId, "Schedules");

        var triggerResult = QuartzTriggers.Build(schedule, triggerKey, jobKey: null, NullLogger.Instance);
        if (triggerResult.IsFailure)
            return Task.FromResult(triggerResult.ToNewResult<QuartzNativeAction>());

        var newTrigger = triggerResult.Value!;

        QuartzNativeAction action = async (scheduler, ct) =>
        {
            try
            {
                await scheduler.RescheduleJob(triggerKey, newTrigger, ct).ConfigureAwait(false);
                return GenericResult.Success();
            }
            catch (Exception ex)
            {
                return GenericResult.Failure(QuartzSchedulingLog.NativeCallFailed(NullLogger.Instance, ex, schedule.ScheduleId));
            }
        };

        return Task.FromResult(GenericResult<QuartzNativeAction>.Success(action));
    }
}
