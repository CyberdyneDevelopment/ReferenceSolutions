using System.Collections.Generic;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Scheduling;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Endpoints;
using FdwReferenceSchedules.Endpoints.Logging;

namespace FdwReferenceSchedules.Endpoints;

/// <summary>
/// Endpoint to update an existing schedule.
/// Sealed closure of generic base class from Fdw.Services.Scheduling.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdateScheduleEndpoint : UpdateScheduleEndpointBase<IScheduleImplementationConfiguration>
{
    /// <inheritdoc />
    public UpdateScheduleEndpoint(ScheduleConfigurationProvider provider, IRoleGrants grants, ILogger<UpdateScheduleEndpoint> logger)
        : base(logger, provider, grants) {
    }

    /// <summary>Maps existing configuration to detail DTO for the find phase.</summary>
    protected override ScheduleDetailDto MapExistingToDetail(IScheduleImplementationConfiguration config) =>
        ScheduleDtoMapper.Detail(config);

    /// <summary>Merges the update request into the existing configuration.</summary>
    protected override IScheduleImplementationConfiguration MergeUpdate(UpdateScheduleRequest request, IScheduleImplementationConfiguration existing, IReadOnlyList<string>? runRoles) =>
        // One list says which members an update states and how each reaches the stored schedule (FDW-1170).
        UpdateScheduleChanges.Apply(request, existing, runRoles);

    /// <summary>Maps the saved updated configuration to a detail DTO.</summary>
    protected override ScheduleDetailDto MapUpdatedToDetail(IScheduleImplementationConfiguration savedConfig, IScheduleImplementationConfiguration updatedConfig) =>
        ScheduleDtoMapper.Detail(savedConfig);

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
