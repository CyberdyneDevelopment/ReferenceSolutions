using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Scheduling;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Endpoints;
using ReferenceSchedules.Endpoints.Logging;

namespace ReferenceSchedules.Endpoints;

/// <summary>
/// Endpoint to create a new schedule.
/// Sealed closure of generic base class from Fdw.Services.Scheduling.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class CreateScheduleEndpoint : CreateScheduleEndpointBase<ScheduleConfiguration>
{
    /// <inheritdoc />
    public CreateScheduleEndpoint(ScheduleConfigurationProvider provider)
        : base(provider)
    {
    }

    /// <summary>Builds schedule configuration from create request.</summary>
    protected override ScheduleConfiguration CreateConfiguration(CreateScheduleRequest request, Guid scheduleId)
    {
        // Why: CronScheduleConfiguration and IntervalScheduleConfiguration were removed in
        // FDW polymorphic-config refactor. ScheduleConfiguration is now a single flat type;
        // ServiceOptionType is the discriminator that drives runtime dispatch (Cron vs Interval).
        return new ScheduleConfiguration
        {
            Id = scheduleId,
            Name = request.Name,
            PipelineName = request.PipelineName,
            IsEnabled = request.IsEnabled,
            ServiceOptionType = request.SchedulerType,
            CronExpression = request.CronExpression,
            IntervalSeconds = request.IntervalSeconds,
            OneTimeDateTime = request.OneTimeDateTime,
            EventName = request.EventName,
            TimeZoneId = request.TimeZoneId
        };
    }

    /// <summary>Maps saved configuration to detail DTO.</summary>
    protected override ScheduleDetailDto MapToDetail(ScheduleConfiguration savedConfig, CreateScheduleRequest request, Guid scheduleId)
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
    protected override void OnBeforeCreate(string resourceName)
    {
        // Why: PipelineName is not yet available at this hook — log the name as primary identifier.
        ScheduleLog.CreatingSchedule(Logger, resourceName, string.Empty);
    }

    /// <inheritdoc />
    protected override void OnAlreadyExists(string resourceName)
    {
        ScheduleLog.ScheduleAlreadyExists(Logger, resourceName);
    }

    /// <inheritdoc />
    protected override void OnAfterCreate(string resourceName)
    {
        ScheduleLog.ScheduleCreated(Logger, resourceName);
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Schedules");
    }
}
