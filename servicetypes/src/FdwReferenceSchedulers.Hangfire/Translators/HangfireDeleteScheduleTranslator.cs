using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Scheduling.Abstractions;
using FdwReferenceSchedulers.Hangfire.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceSchedulers.Hangfire.Translators;

/// <summary>Translates <see cref="DeleteScheduleCommand"/> into a Hangfire <c>RemoveIfExists</c> call.</summary>
[TypeOption(typeof(HangfireSchedulingTranslators), "Delete")]
public sealed class HangfireDeleteScheduleTranslator : HangfireSchedulingTranslatorBase
{
    /// <summary>Initializes a new instance of the <see cref="HangfireDeleteScheduleTranslator"/> class.</summary>
    public HangfireDeleteScheduleTranslator() : base("Delete")
    {
    }

    /// <inheritdoc />
    public override Task<IGenericResult<HangfireNativeAction>> Translate(
        ISchedulingCommand command, CancellationToken cancellationToken = default)
    {
        if (command is not DeleteScheduleCommand delete)
        {
            return Task.FromResult(GenericResult<HangfireNativeAction>.Failure(
                HangfireSchedulingLog.NativeCallFailed(NullLogger.Instance, new InvalidOperationException(
                    $"HangfireDeleteScheduleTranslator cannot translate {command.GetType().Name}"), "(unknown)")));
        }

        HangfireNativeAction action = manager =>
        {
            try
            {
                manager.RemoveIfExists(delete.ScheduleId);
                return GenericResult.Success();
            }
            catch (Exception ex)
            {
                return GenericResult.Failure(HangfireSchedulingLog.NativeCallFailed(NullLogger.Instance, ex, delete.ScheduleId));
            }
        };

        return Task.FromResult(GenericResult<HangfireNativeAction>.Success(action));
    }
}
