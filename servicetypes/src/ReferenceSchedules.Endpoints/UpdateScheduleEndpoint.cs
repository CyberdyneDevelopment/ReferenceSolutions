using Fdw.Services.Scheduling;
using System.Diagnostics.CodeAnalysis;

using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Endpoints;
using ReferenceSchedules.Endpoints.Logging;

namespace ReferenceSchedules.Endpoints;

/// <summary>
/// Endpoint to update an existing schedule.
/// Sealed closure of generic base class from Fdw.Services.Scheduling.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdateScheduleEndpoint : UpdateScheduleEndpointBase<ScheduleConfiguration>
{
    /// <inheritdoc />
    public UpdateScheduleEndpoint(
        ScheduleConfigurationProvider provider)
        : base(provider)
    {
    }

    /// <summary>Maps existing configuration to detail DTO for the find phase.</summary>
    protected override ScheduleDetailDto MapExistingToDetail(ScheduleConfiguration config)
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

    /// <summary>Merges the update request into the existing configuration.</summary>
    protected override ScheduleConfiguration MergeUpdate(UpdateScheduleRequest request, ScheduleConfiguration existing)
    {
        if (request.PipelineName is not null)
            existing.PipelineName = request.PipelineName;
        if (request.IsEnabled is not null)
            existing.IsEnabled = request.IsEnabled.Value;

        if (request.CronExpression != null)
            existing.CronExpression = request.CronExpression;

        if (request.IntervalSeconds.HasValue)
            existing.IntervalSeconds = request.IntervalSeconds.Value;

        return existing;
    }

    /// <summary>Maps the saved updated configuration to a detail DTO.</summary>
    protected override ScheduleDetailDto MapUpdatedToDetail(ScheduleConfiguration savedConfig, ScheduleConfiguration updatedConfig)
    {
        return new ScheduleDetailDto
        {
            Id = savedConfig.Id,
            Name = savedConfig.Name,
            PipelineName = savedConfig.PipelineName,
            SchedulerType = savedConfig.ScheduleType,
            IsEnabled = savedConfig.IsEnabled,
            NextRunTime = savedConfig.NextRunTime,
            LastRunTime = savedConfig.LastRunTime,
            CronExpression = savedConfig.CronExpression,
            IntervalSeconds = savedConfig.IntervalSeconds
        };
    }

    /// <inheritdoc />
    protected override void OnBeforeUpdate(string identifier)
    {
        ScheduleLog.UpdatingSchedule(Logger, identifier);
    }

    /// <inheritdoc />
    protected override void OnNotFound(string identifier)
    {
        ScheduleLog.ScheduleNotFound(Logger, identifier);
    }

    /// <inheritdoc />
    protected override void OnAfterUpdate(string identifier)
    {
        ScheduleLog.ScheduleUpdated(Logger, identifier);
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Schedules");
    }
}
