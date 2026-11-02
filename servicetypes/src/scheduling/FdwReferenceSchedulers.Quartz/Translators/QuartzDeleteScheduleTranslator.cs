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

/// <summary>Translates <see cref="DeleteScheduleCommand"/> into a Quartz <c>DeleteJob</c> call.</summary>
[TypeOption(typeof(QuartzSchedulingTranslators), "Delete")]
public sealed class QuartzDeleteScheduleTranslator : QuartzSchedulingTranslatorBase
{
    /// <summary>Initializes a new instance of the <see cref="QuartzDeleteScheduleTranslator"/> class.</summary>
    public QuartzDeleteScheduleTranslator() : base("Delete")
    {
    }

    /// <inheritdoc />
    public override Task<IGenericResult<QuartzNativeAction>> Translate(
        ISchedulingCommand command, CancellationToken cancellationToken = default)
    {
        if (command is not DeleteScheduleCommand delete)
        {
            return Task.FromResult(GenericResult<QuartzNativeAction>.Failure(
                QuartzSchedulingLog.NativeCallFailed(NullLogger.Instance, new InvalidOperationException(
                    $"QuartzDeleteScheduleTranslator cannot translate {command.GetType().Name}"), "(unknown)")));
        }

        var jobKey = new JobKey(delete.ScheduleId, "Schedules");

        QuartzNativeAction action = async (scheduler, ct) =>
        {
            try
            {
                await scheduler.DeleteJob(jobKey, ct).ConfigureAwait(false);
                return GenericResult.Success();
            }
            catch (Exception ex)
            {
                return GenericResult.Failure(QuartzSchedulingLog.NativeCallFailed(NullLogger.Instance, ex, delete.ScheduleId));
            }
        };

        return Task.FromResult(GenericResult<QuartzNativeAction>.Success(action));
    }
}
