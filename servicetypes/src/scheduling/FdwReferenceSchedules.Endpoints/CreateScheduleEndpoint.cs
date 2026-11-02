using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Scheduling;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Endpoints;
using FdwReferenceSchedules.Endpoints.Logging;

namespace FdwReferenceSchedules.Endpoints;

/// <summary>
/// Endpoint to create a new schedule.
/// Sealed closure of generic base class from Fdw.Services.Scheduling.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class CreateScheduleEndpoint : CreateScheduleEndpointBase<IScheduleImplementationConfiguration>
{
    /// <inheritdoc />
    public CreateScheduleEndpoint(ScheduleConfigurationProvider provider, IRoleGrants grants, ILogger<CreateScheduleEndpoint> logger)
        : base(logger, provider, grants) {
    }

    /// <summary>Builds schedule configuration from create request.</summary>
    protected override IScheduleImplementationConfiguration CreateConfiguration(CreateScheduleRequest request, Guid scheduleId, IReadOnlyList<string> runRoles)
    {
        // Why: CronScheduleConfiguration and IntervalScheduleConfiguration were removed in
        // FDW polymorphic-config refactor. IScheduleImplementationConfiguration is now a single flat type;
        // Implementation is the discriminator that drives runtime dispatch (Cron vs Interval).
        var schedule = new ScheduleImplementationConfiguration
        {
            Id = scheduleId,
            Name = request.Name,
            PipelineId = request.PipelineId,
            PipelineName = request.PipelineName,
            IsEnabled = request.IsEnabled,
            Implementation = request.SchedulerType,
            CronExpression = request.CronExpression,
            IntervalSeconds = request.IntervalSeconds,
            OneTimeDateTime = request.OneTimeDateTime,
            EventName = request.EventName,
            TimeZoneId = request.TimeZoneId,
            Parameters = ScheduleParameterMapper.ToConfiguration(request.Parameters),
        };
        schedule.ReplaceRunRoles(runRoles);
        return schedule;
    }

    /// <summary>Maps saved configuration to detail DTO.</summary>
    protected override ScheduleDetailDto MapToDetail(IScheduleImplementationConfiguration savedConfig, CreateScheduleRequest request, Guid scheduleId) =>
        ScheduleDtoMapper.Detail(savedConfig);

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
