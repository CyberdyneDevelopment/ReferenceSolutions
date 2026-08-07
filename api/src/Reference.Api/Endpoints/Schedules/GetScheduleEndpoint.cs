using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Scheduling;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Endpoints;
using Reference.Api.Logging;

namespace Reference.Api.Endpoints;

/// <summary>
/// Endpoint to get schedule details by name.
/// Sealed closure of generic base class from Fdw.Services.Scheduling.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetScheduleEndpoint : GetScheduleEndpointBase<ScheduleConfiguration>
{
    /// <inheritdoc />
    public GetScheduleEndpoint(ScheduleConfigurationProvider provider)
        : base(provider)
    {
    }

    /// <summary>Maps schedule configuration to detail DTO.</summary>
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
    protected override void OnBeforeGet(string identifier)
    {
        ScheduleLog.FetchingSchedule(Logger, identifier);
    }

    /// <inheritdoc />
    protected override void OnNotFound(string identifier)
    {
        ScheduleLog.ScheduleNotFound(Logger, identifier);
    }

    /// <inheritdoc />
    protected override void OnAfterGet(string identifier)
    {
        ScheduleLog.ScheduleRetrieved(Logger, identifier);
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Schedules");
    }
}
