using Fdw.Services.Scheduling;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Endpoints;
using FdwReferenceSchedules.Endpoints.Logging;

namespace FdwReferenceSchedules.Endpoints;

/// <summary>
/// Endpoint to toggle schedule enabled status.
/// Sealed closure of generic base class from Fdw.Services.Scheduling.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class ToggleScheduleEndpoint : ToggleScheduleEndpointBase<IScheduleImplementationConfiguration>
{
    /// <inheritdoc />
    public ToggleScheduleEndpoint(ScheduleConfigurationProvider provider, ILogger<ToggleScheduleEndpoint> logger)
        : base(logger, provider) {
    }

    /// <inheritdoc />
    protected override IScheduleImplementationConfiguration UpdateEnabledStatus(IScheduleImplementationConfiguration config, bool isEnabled)
    {
        if (isEnabled)
            ScheduleLog.ScheduleEnabled(Logger, config.Name);
        else
            ScheduleLog.ScheduleDisabled(Logger, config.Name);

        config.IsEnabled = isEnabled;
        return config;
    }

    /// <summary>Maps the saved configuration to a detail DTO.</summary>
    protected override ScheduleDetailDto MapToDetail(IScheduleImplementationConfiguration config) =>
        ScheduleDtoMapper.Detail(config);

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Schedules");
    }
}
