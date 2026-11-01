using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Scheduling;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Endpoints;
using FdwReferenceSchedules.Endpoints.Logging;

namespace FdwReferenceSchedules.Endpoints;

/// <summary>
/// Endpoint to get schedule details by name.
/// Sealed closure of generic base class from Fdw.Services.Scheduling.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetScheduleEndpoint : GetScheduleEndpointBase<IScheduleImplementationConfiguration>
{
    /// <inheritdoc />
    public GetScheduleEndpoint(ScheduleConfigurationProvider provider, ILogger<GetScheduleEndpoint> logger)
        : base(logger, provider) {
    }

    /// <summary>Maps schedule configuration to detail DTO.</summary>
    protected override ScheduleDetailDto MapToDetail(IScheduleImplementationConfiguration config) =>
        ScheduleDtoMapper.Detail(config);

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
