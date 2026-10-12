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
/// Translates <see cref="TriggerScheduleCommand"/> into a Hangfire immediate-trigger call.
/// </summary>
/// <remarks>
/// Flagged for review: <c>IRecurringJobManager.Trigger</c> is the current API at time of writing;
/// newer Hangfire versions may rename this to <c>TriggerJob</c> — verify against the actual
/// restored package before trusting this in production.
/// </remarks>
[TypeOption(typeof(HangfireSchedulingTranslators), "Trigger")]
public sealed class HangfireTriggerScheduleTranslator : HangfireSchedulingTranslatorBase
{
    /// <summary>Initializes a new instance of the <see cref="HangfireTriggerScheduleTranslator"/> class.</summary>
    public HangfireTriggerScheduleTranslator() : base("Trigger")
    {
    }

    /// <inheritdoc />
    public override Task<IGenericResult<HangfireNativeAction>> Translate(
        ISchedulingCommand command, CancellationToken cancellationToken = default)
    {
        if (command is not TriggerScheduleCommand trigger)
        {
            return Task.FromResult(GenericResult<HangfireNativeAction>.Failure(
                HangfireSchedulingLog.NativeCallFailed(NullLogger.Instance, new InvalidOperationException(
                    $"HangfireTriggerScheduleTranslator cannot translate {command.GetType().Name}"), "(unknown)")));
        }

        HangfireNativeAction action = manager =>
        {
            try
            {
                manager.Trigger(trigger.ScheduleId);
                return GenericResult.Success();
            }
            catch (Exception ex)
            {
                return GenericResult.Failure(HangfireSchedulingLog.NativeCallFailed(NullLogger.Instance, ex, trigger.ScheduleId));
            }
        };

        return Task.FromResult(GenericResult<HangfireNativeAction>.Success(action));
    }
}
