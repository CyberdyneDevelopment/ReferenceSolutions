using Fdw.Services.Scheduling;
using System.Diagnostics.CodeAnalysis;

using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Endpoints;
using ReferenceSchedules.Endpoints.Logging;

namespace ReferenceSchedules.Endpoints;

/// <summary>
/// Endpoint to toggle schedule enabled status.
/// Sealed closure of generic base class from Fdw.Services.Scheduling.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class ToggleScheduleEndpoint : ToggleScheduleEndpointBase<ScheduleConfiguration>
{
    /// <inheritdoc />
    public ToggleScheduleEndpoint(
        ScheduleConfigurationProvider provider)
        : base(provider)
    {
    }

    /// <inheritdoc />
    protected override ScheduleConfiguration UpdateEnabledStatus(ScheduleConfiguration config, bool isEnabled)
    {
        if (isEnabled)
            ScheduleLog.ScheduleEnabled(Logger, config.Name);
        else
            ScheduleLog.ScheduleDisabled(Logger, config.Name);

        config.IsEnabled = isEnabled;
        return config;
    }

    /// <summary>Maps the saved configuration to a detail DTO.</summary>
    protected override ScheduleDetailDto MapToDetail(ScheduleConfiguration config)
    {
        return new ScheduleDetailDto
        {
            Id = config.Id,
            Name = config.Name,
            PipelineName = config.PipelineName,
            SchedulerType = config.ScheduleType,
            IsEnabled = config.IsEnabled,
            NextRunTime = config.NextRunTime,
            LastRunTime = config.LastRunTime,
            CronExpression = config.CronExpression,
            IntervalSeconds = config.IntervalSeconds
        };
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Schedules");
    }
}
