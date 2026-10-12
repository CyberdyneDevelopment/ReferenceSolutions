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

/// <summary>Translates <see cref="TriggerScheduleCommand"/> into a Quartz <c>TriggerJob</c> call.</summary>
[TypeOption(typeof(QuartzSchedulingTranslators), "Trigger")]
public sealed class QuartzTriggerScheduleTranslator : QuartzSchedulingTranslatorBase
{
    /// <summary>Initializes a new instance of the <see cref="QuartzTriggerScheduleTranslator"/> class.</summary>
    public QuartzTriggerScheduleTranslator() : base("Trigger")
    {
    }

    /// <inheritdoc />
    public override Task<IGenericResult<QuartzNativeAction>> Translate(
        ISchedulingCommand command, CancellationToken cancellationToken = default)
    {
        if (command is not TriggerScheduleCommand trigger)
        {
            return Task.FromResult(GenericResult<QuartzNativeAction>.Failure(
                QuartzSchedulingLog.NativeCallFailed(NullLogger.Instance, new InvalidOperationException(
                    $"QuartzTriggerScheduleTranslator cannot translate {command.GetType().Name}"), "(unknown)")));
        }

        var jobKey = new JobKey(trigger.ScheduleId, "Schedules");

        QuartzNativeAction action = async (scheduler, ct) =>
        {
            try
            {
                await scheduler.TriggerJob(jobKey, cancellationToken: ct).ConfigureAwait(false);
                return GenericResult.Success();
            }
            catch (Exception ex)
            {
                return GenericResult.Failure(QuartzSchedulingLog.NativeCallFailed(NullLogger.Instance, ex, trigger.ScheduleId));
            }
        };

        return Task.FromResult(GenericResult<QuartzNativeAction>.Success(action));
    }
}
