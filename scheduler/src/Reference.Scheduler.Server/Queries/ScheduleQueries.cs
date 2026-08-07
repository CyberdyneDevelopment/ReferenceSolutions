using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Extensions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Scheduling;
using Fdw.Services.Scheduling.Data;
using Reference.Scheduler.Server.Models;

namespace Reference.Scheduler.Server.Queries;

/// <summary>
/// Static helpers for building schedule data commands and mapping records to DTOs.
/// Queries and updates are executed directly through <see cref="IDataGateway"/> using
/// the target-typed Execute overloads introduced in FDW 1.5.0.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ScheduleQueries
{
    /// <summary>
    /// Builds a gateway call to list all schedules, optionally filtered by tenant.
    /// </summary>
    internal static DataGatewayCall Get(
        SchedulerConfiguration config,
        ITenantContext? tenantContext = null)
    {
        var query = Query.From<ScheduleQueryRecord>(
            config.DataStoreName, config.PathName, config.ScheduleContainerName);

        if (tenantContext?.HasTenant == true && tenantContext.TenantId.HasValue)
        {
            query = query.BeginOrGroup()
                .Where(s => s.TenantId).Equal(tenantContext.TenantId.Value)
                .Where(s => s.TenantId).IsNull()
                .EndGroup();
        }

        return query.OrderBy("Name").Build();
    }

    /// <summary>
    /// Builds a gateway call to get a single schedule by name, optionally filtered by tenant.
    /// </summary>
    internal static DataGatewayCall Get(
        SchedulerConfiguration config,
        string scheduleName,
        ITenantContext? tenantContext = null)
    {
        var query = Query.From<ScheduleQueryRecord>(
                config.DataStoreName, config.PathName, config.ScheduleContainerName)
            .Where("Name", scheduleName);

        if (tenantContext?.HasTenant == true && tenantContext.TenantId.HasValue)
        {
            query = query.BeginOrGroup()
                .Where(s => s.TenantId).Equal(tenantContext.TenantId.Value)
                .Where(s => s.TenantId).IsNull()
                .EndGroup();
        }

        return query.Build();
    }

    /// <summary>
    /// Builds a gateway call for updating a schedule's run bookkeeping (LastRunTime, NextRunTime,
    /// LastRunStatus). The status carries the dispatch outcome — see <see cref="ScheduleStatuses"/>.
    /// </summary>
    internal static DataGatewayCall UpdateTimestamps(
        SchedulerConfiguration config,
        string scheduleName,
        DateTimeOffset? lastRunTime,
        DateTimeOffset? nextRunTime,
        string lastRunStatus)
    {
        var record = new ScheduleUpdateRecord
        {
            LastRunTime = lastRunTime,
            NextRunTime = nextRunTime,
            LastRunStatus = lastRunStatus
        };

        return Update.In<ScheduleUpdateRecord>(config.ScheduleContainerName)
            .DataStore(config.DataStoreName)
            .Path(config.PathName)
            .Where("Name", scheduleName)
            .Value(record);
    }

    /// <summary>
    /// Maps a <see cref="ScheduleQueryRecord"/> to a <see cref="ScheduleInfo"/> DTO.
    /// </summary>
    internal static ScheduleInfo ToScheduleInfo(ScheduleQueryRecord record)
    {
        return new ScheduleInfo(
            Name: record.Name,
            PipelineName: record.PipelineName,
            ServiceOptionType: record.ServiceOptionType,
            CronExpression: record.CronExpression,
            IntervalSeconds: record.IntervalSeconds,
            TimeZoneId: record.TimeZoneId,
            IsEnabled: record.IsEnabled,
            LastRunTime: record.LastRunTime,
            NextRunTime: record.NextRunTime);
    }
}
