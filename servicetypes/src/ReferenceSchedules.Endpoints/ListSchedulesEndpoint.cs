using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Endpoints;
using ReferenceSchedules.Endpoints.Logging;

namespace ReferenceSchedules.Endpoints;

/// <summary>
/// Endpoint to list schedules with pagination.
/// Sealed closure of generic base class from Fdw.Services.Scheduling.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListSchedulesEndpoint : ListSchedulesEndpointBase
{
    /// <inheritdoc />
    public ListSchedulesEndpoint(IServiceConfigurationProvider<ScheduleConfiguration> provider)
        : base(provider)
    {
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<List<ScheduleSummaryDto>>> LoadItems(ListSchedulesRequest request, CancellationToken ct)
    {
        ScheduleLog.ListingSchedules(Logger, request.ValidatedPage, request.ValidatedPageSize);
        var result = await base.LoadItems(request, ct).ConfigureAwait(false);
        if (result.IsSuccess)
            ScheduleLog.SchedulesListed(Logger, result.Value?.Count ?? 0);
        return result;
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Schedules");
    }
}
