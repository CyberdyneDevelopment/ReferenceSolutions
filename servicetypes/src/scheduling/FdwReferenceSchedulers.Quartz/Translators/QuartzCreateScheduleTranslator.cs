using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Scheduling.Abstractions;
using Quartz;
using FdwReferenceSchedulers.Quartz.Logging;
using FdwReferenceSchedulers.Quartz.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceSchedulers.Quartz.Translators;

/// <summary>Translates <see cref="CreateScheduleCommand"/> into a Quartz <c>ScheduleJob</c> call.</summary>
[TypeOption(typeof(QuartzSchedulingTranslators), "Create")]
public sealed class QuartzCreateScheduleTranslator : QuartzSchedulingTranslatorBase
{
    /// <summary>Initializes a new instance of the <see cref="QuartzCreateScheduleTranslator"/> class.</summary>
    public QuartzCreateScheduleTranslator() : base("Create")
    {
    }

    /// <inheritdoc />
    public override Task<IGenericResult<QuartzNativeAction>> Translate(
        ISchedulingCommand command, CancellationToken cancellationToken = default)
    {
        if (command is not CreateScheduleCommand create)
        {
            return Task.FromResult(GenericResult<QuartzNativeAction>.Failure(
                QuartzSchedulingLog.NativeCallFailed(NullLogger.Instance, new InvalidOperationException(
                    $"QuartzCreateScheduleTranslator cannot translate {command.GetType().Name}"), "(unknown)")));
        }

        var schedule = create.Schedule;
        var jobKey = new JobKey(schedule.ScheduleId, "Schedules");
        var triggerKey = new TriggerKey(schedule.ScheduleId, "Schedules");

        var jobDetail = JobBuilder.Create<ScheduledPipelineJob>()
            .WithIdentity(jobKey)
            .UsingJobData(ScheduledPipelineJob.ProcessIdKey, schedule.ProcessId)
            .UsingJobData(ScheduledPipelineJob.ScheduleIdKey, schedule.ScheduleId)
            .UsingJobData(ScheduledPipelineJob.DataStoreNameKey, create.DataStoreName)
            .UsingJobData(ScheduledPipelineJob.PathNameKey, create.PathName)
            .UsingJobData(ScheduledPipelineJob.ScheduleContainerNameKey, create.ScheduleContainerName)
            .StoreDurably()
            .Build();

        var triggerResult = QuartzTriggers.Build(schedule, triggerKey, jobKey, NullLogger.Instance);
        if (triggerResult.IsFailure)
            return Task.FromResult(triggerResult.ToNewResult<QuartzNativeAction>());

        var trigger = triggerResult.Value!;

        QuartzNativeAction action = async (scheduler, ct) =>
        {
            try
            {
                await scheduler.ScheduleJob(jobDetail, trigger, ct).ConfigureAwait(false);
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
