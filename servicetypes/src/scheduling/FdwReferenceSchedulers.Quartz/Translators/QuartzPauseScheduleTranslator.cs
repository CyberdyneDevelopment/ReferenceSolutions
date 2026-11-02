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

/// <summary>Translates <see cref="PauseScheduleCommand"/> into a Quartz <c>PauseTrigger</c> call.</summary>
[TypeOption(typeof(QuartzSchedulingTranslators), "Pause")]
public sealed class QuartzPauseScheduleTranslator : QuartzSchedulingTranslatorBase
{
    /// <summary>Initializes a new instance of the <see cref="QuartzPauseScheduleTranslator"/> class.</summary>
    public QuartzPauseScheduleTranslator() : base("Pause")
    {
    }

    /// <inheritdoc />
    public override Task<IGenericResult<QuartzNativeAction>> Translate(
        ISchedulingCommand command, CancellationToken cancellationToken = default)
    {
        if (command is not PauseScheduleCommand pause)
        {
            return Task.FromResult(GenericResult<QuartzNativeAction>.Failure(
                QuartzSchedulingLog.NativeCallFailed(NullLogger.Instance, new InvalidOperationException(
                    $"QuartzPauseScheduleTranslator cannot translate {command.GetType().Name}"), "(unknown)")));
        }

        var triggerKey = new TriggerKey(pause.ScheduleId, "Schedules");

        QuartzNativeAction action = async (scheduler, ct) =>
        {
            try
            {
                await scheduler.PauseTrigger(triggerKey, ct).ConfigureAwait(false);
                return GenericResult.Success();
            }
            catch (Exception ex)
            {
                return GenericResult.Failure(QuartzSchedulingLog.NativeCallFailed(NullLogger.Instance, ex, pause.ScheduleId));
            }
        };

        return Task.FromResult(GenericResult<QuartzNativeAction>.Success(action));
    }
}
