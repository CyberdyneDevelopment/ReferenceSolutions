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

/// <summary>Translates <see cref="ResumeScheduleCommand"/> into a Quartz <c>ResumeTrigger</c> call.</summary>
[TypeOption(typeof(QuartzSchedulingTranslators), "Resume")]
public sealed class QuartzResumeScheduleTranslator : QuartzSchedulingTranslatorBase
{
    /// <summary>Initializes a new instance of the <see cref="QuartzResumeScheduleTranslator"/> class.</summary>
    public QuartzResumeScheduleTranslator() : base("Resume")
    {
    }

    /// <inheritdoc />
    public override Task<IGenericResult<QuartzNativeAction>> Translate(
        ISchedulingCommand command, CancellationToken cancellationToken = default)
    {
        if (command is not ResumeScheduleCommand resume)
        {
            return Task.FromResult(GenericResult<QuartzNativeAction>.Failure(
                QuartzSchedulingLog.NativeCallFailed(NullLogger.Instance, new InvalidOperationException(
                    $"QuartzResumeScheduleTranslator cannot translate {command.GetType().Name}"), "(unknown)")));
        }

        var triggerKey = new TriggerKey(resume.ScheduleId, "Schedules");

        QuartzNativeAction action = async (scheduler, ct) =>
        {
            try
            {
                await scheduler.ResumeTrigger(triggerKey, ct).ConfigureAwait(false);
                return GenericResult.Success();
            }
            catch (Exception ex)
            {
                return GenericResult.Failure(QuartzSchedulingLog.NativeCallFailed(NullLogger.Instance, ex, resume.ScheduleId));
            }
        };

        return Task.FromResult(GenericResult<QuartzNativeAction>.Success(action));
    }
}
