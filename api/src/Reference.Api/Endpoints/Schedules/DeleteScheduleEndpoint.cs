using System.Collections.Generic;
using Fdw.Services.Scheduling;
using System.Diagnostics.CodeAnalysis;

using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Endpoints;
using Microsoft.Extensions.Options;
using Reference.Api.Logging;

namespace Reference.Api.Endpoints;

/// <summary>
/// Endpoint to delete a schedule.
/// Sealed closure of generic base class from Fdw.Services.Scheduling.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteScheduleEndpoint : DeleteScheduleEndpointBase<ScheduleConfiguration>
{
    /// <inheritdoc />
    public DeleteScheduleEndpoint(
        ScheduleConfigurationProvider provider)
        : base(provider)
    {
    }

    /// <inheritdoc />
    protected override void OnBeforeDelete(string identifier)
    {
        ScheduleLog.DeletingSchedule(Logger, identifier);
    }

    /// <inheritdoc />
    protected override void OnNotFound(string identifier)
    {
        ScheduleLog.ScheduleNotFound(Logger, identifier);
    }

    /// <inheritdoc />
    protected override void OnAfterDelete(string identifier)
    {
        ScheduleLog.ScheduleDeleted(Logger, identifier);
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Schedules");
    }
}
