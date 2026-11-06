using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Endpoints;
using Fdw.Services.Tags;
using FdwReferenceSchedules.Endpoints.Logging;

namespace FdwReferenceSchedules.Endpoints;

/// <summary>
/// Endpoint to list schedules with pagination.
/// Sealed closure of generic base class from Fdw.Services.Scheduling.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListSchedulesEndpoint : ListSchedulesEndpointBase
{
    /// <inheritdoc />
    public ListSchedulesEndpoint(IScheduleConfigurationProvider provider, ILogger<ListSchedulesEndpoint> logger, ITagService? tags = null)
        : base(logger, provider, tags) {
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<List<ScheduleSummaryDto>>> LoadItems(ListSchedulesRequest request, CancellationToken ct)
    {
        ScheduleLog.ListingSchedules(Logger, request.Page, request.PageSize);
        var result = await base.LoadItems(request, ct).ConfigureAwait(false);
        if (result.IsSuccess && result.Value is { } schedules)
            ScheduleLog.SchedulesListed(Logger, schedules.Count);
        return result;
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Schedules");
    }
}
