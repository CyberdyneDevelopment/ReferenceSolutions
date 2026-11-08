using System;

namespace FdwReferenceSchedulers.Quartz.Services;

/// <summary>
/// Which kind of fire a trigger's data describes (FDW-4387): see <see cref="ScheduledPipelineJob.ReadFireKind"/>.
/// </summary>
/// <param name="TriggerType">How the fire is recorded: Scheduled, Catchup or Retry.</param>
/// <param name="ScheduledFireTime">The time the fire was scheduled for when its data says so (a catch-up's due time, a retried fire's time); null for an ordinary fire, which takes the time Quartz scheduled it for.</param>
/// <param name="RetryAttempt">The attempt number of a retry; 0 for a fire that is not a retry.</param>
/// <param name="UnreadableKey">The job data key that is present but cannot be read, or null when the data reads; the fire records nothing when it is set.</param>
public sealed record ScheduledFireKind(string TriggerType, DateTime? ScheduledFireTime, int RetryAttempt, string? UnreadableKey);
