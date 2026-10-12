using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Scheduling.Abstractions;
using FdwReferenceSchedulers.Hangfire.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceSchedulers.Hangfire.Translators;

/// <summary>
/// Translates <see cref="ResumeScheduleCommand"/> for Hangfire.
/// </summary>
/// <remarks>
/// Flagged gap: unlike Quartz (which keeps a paused trigger registered, so <c>ResumeTrigger</c> needs
/// only the id), Hangfire's <see cref="HangfirePauseScheduleTranslator"/> removes the recurring job
/// entirely — resuming it means re-adding the full definition (process id, cron expression, time
/// zone), which <see cref="ResumeScheduleCommand"/> does not carry, only a schedule id. This fails
/// loud rather than silently resuming with no schedule attached. Fix needs either widening
/// <see cref="ResumeScheduleCommand"/> to carry the full <see cref="IGenericSchedule"/> (the caller
/// would look it up before building the command) or the service resolving the definition itself
/// before translating.
/// </remarks>
[TypeOption(typeof(HangfireSchedulingTranslators), "Resume")]
public sealed class HangfireResumeScheduleTranslator : HangfireSchedulingTranslatorBase
{
    /// <summary>Initializes a new instance of the <see cref="HangfireResumeScheduleTranslator"/> class.</summary>
    public HangfireResumeScheduleTranslator() : base("Resume")
    {
    }

    /// <inheritdoc />
    public override Task<IGenericResult<HangfireNativeAction>> Translate(
        ISchedulingCommand command, CancellationToken cancellationToken = default)
    {
        if (command is not ResumeScheduleCommand resume)
        {
            return Task.FromResult(GenericResult<HangfireNativeAction>.Failure(
                HangfireSchedulingLog.NativeCallFailed(NullLogger.Instance, new InvalidOperationException(
                    $"HangfireResumeScheduleTranslator cannot translate {command.GetType().Name}"), "(unknown)")));
        }

        return Task.FromResult(GenericResult<HangfireNativeAction>.Failure(
            HangfireSchedulingLog.NativeCallFailed(NullLogger.Instance,
                new NotSupportedException(
                    "Hangfire cannot resume from a schedule id alone: pausing removes the recurring job entirely, " +
                    "so resuming needs the full schedule definition to re-add it. See remarks on HangfireResumeScheduleTranslator."),
                resume.ScheduleId)));
    }
}
