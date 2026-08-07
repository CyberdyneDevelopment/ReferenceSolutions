using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Abstractions;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Microsoft.Extensions.Logging;
using Reference.Scheduler.Server.Logging;
using Reference.Scheduler.Server.Models;

namespace Reference.Scheduler.Server.Endpoints;

[ExcludeFromCodeCoverage]
public sealed class ListSchedulesEndpoint : EndpointWithoutRequest<IReadOnlyList<ScheduleInfo>>
{
    // Why: IServiceConfigurationProvider<ScheduleConfiguration> replaces IOptionsMonitor<List<SchedulerConfiguration>>
    // + raw DataGateway queries. The provider already merges ctrl + cfg schedule data via the dual-source pattern.
    private readonly IServiceConfigurationProvider<ScheduleConfiguration> _scheduleProvider;
    private readonly ILogger<ListSchedulesEndpoint> _logger;

    public ListSchedulesEndpoint(
        IServiceConfigurationProvider<ScheduleConfiguration> scheduleProvider,
        ILogger<ListSchedulesEndpoint> logger)
    {
        _scheduleProvider = scheduleProvider;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("schedules");
        // Why: Scheduler endpoints are anonymous — authentication is enforced at the service boundary (bearer token from callers).
        Policies("schedules:read");
        Summary(s =>
        {
            s.Summary = "List all schedules";
            s.Description = "Returns all configured schedules.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        SchedulerServerLog.ListSchedulesRequestReceived(_logger);

        var allResult = await _scheduleProvider.Get(ct).ConfigureAwait(false);
        if (!allResult.IsSuccess || allResult.Value is null)
        {
            await Send.OkAsync((IReadOnlyList<ScheduleInfo>)new List<ScheduleInfo>(), ct).ConfigureAwait(false);
            return;
        }

        var schedules = allResult.Value
            .Select(ScheduleMapping.ToScheduleInfo)
            .ToList();

        SchedulerServerLog.SchedulesLoaded(_logger, schedules.Count);

        await Send.OkAsync((IReadOnlyList<ScheduleInfo>)schedules, ct).ConfigureAwait(false);
    }
}
