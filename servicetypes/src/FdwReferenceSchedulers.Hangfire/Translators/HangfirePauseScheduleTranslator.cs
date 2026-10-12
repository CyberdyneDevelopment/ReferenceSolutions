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
/// Translates <see cref="PauseScheduleCommand"/> into a Hangfire <c>RemoveIfExists</c> call.
/// Hangfire has no native "pause" — a paused recurring job is one that has actually stopped
/// running, so pausing removes it the same way deleting does; the schedule definition itself stays
/// in the domain's own configuration for a later <see cref="ResumeScheduleCommand"/> to re-add.
/// </summary>
[TypeOption(typeof(HangfireSchedulingTranslators), "Pause")]
public sealed class HangfirePauseScheduleTranslator : HangfireSchedulingTranslatorBase
{
    /// <summary>Initializes a new instance of the <see cref="HangfirePauseScheduleTranslator"/> class.</summary>
    public HangfirePauseScheduleTranslator() : base("Pause")
    {
    }

    /// <inheritdoc />
    public override Task<IGenericResult<HangfireNativeAction>> Translate(
        ISchedulingCommand command, CancellationToken cancellationToken = default)
    {
        if (command is not PauseScheduleCommand pause)
        {
            return Task.FromResult(GenericResult<HangfireNativeAction>.Failure(
                HangfireSchedulingLog.NativeCallFailed(NullLogger.Instance, new InvalidOperationException(
                    $"HangfirePauseScheduleTranslator cannot translate {command.GetType().Name}"), "(unknown)")));
        }

        HangfireNativeAction action = manager =>
        {
            try
            {
                manager.RemoveIfExists(pause.ScheduleId);
                return GenericResult.Success();
            }
            catch (Exception ex)
            {
                return GenericResult.Failure(HangfireSchedulingLog.NativeCallFailed(NullLogger.Instance, ex, pause.ScheduleId));
            }
        };

        return Task.FromResult(GenericResult<HangfireNativeAction>.Success(action));
    }
}
