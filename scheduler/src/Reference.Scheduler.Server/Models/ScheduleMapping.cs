using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Scheduling.Abstractions.Configuration;

namespace Reference.Scheduler.Server.Models;

/// <summary>
/// Maps FDW <see cref="ScheduleConfiguration"/> to the API <see cref="ScheduleInfo"/> DTO.
/// </summary>
// Why: Centralizes the ScheduleConfiguration → ScheduleInfo mapping that was previously
// duplicated across ScheduleQueries.ToScheduleInfo (from ScheduleQueryRecord) in each endpoint.
// The provider now returns ScheduleConfiguration directly, so the mapping source changed.
[ExcludeFromCodeCoverage]
internal static class ScheduleMapping
{
    /// <summary>
    /// Maps a <see cref="ScheduleConfiguration"/> to a <see cref="ScheduleInfo"/> DTO.
    /// </summary>
    internal static ScheduleInfo ToScheduleInfo(ScheduleConfiguration config)
    {
        return new ScheduleInfo(
            Name: config.Name,
            PipelineName: config.PipelineName,
            ServiceOptionType: config.ServiceOptionType ?? string.Empty,
            CronExpression: config.CronExpression,
            IntervalSeconds: config.IntervalSeconds,
            TimeZoneId: config.TimeZoneId ?? string.Empty,
            IsEnabled: config.IsEnabled,
            LastRunTime: config.LastRunTime,
            NextRunTime: config.NextRunTime);
    }
}
