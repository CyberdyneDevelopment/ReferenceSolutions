using System.Diagnostics.CodeAnalysis;
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
public sealed class GetScheduleRequest
{
    public string Name { get; set; } = string.Empty;
}

[ExcludeFromCodeCoverage]
public sealed class GetScheduleEndpoint : Endpoint<GetScheduleRequest, ScheduleInfo>
{
    // Why: IServiceConfigurationProvider<ScheduleConfiguration> replaces IOptionsMonitor<List<SchedulerConfiguration>>
    // + raw DataGateway queries. The provider already merges ctrl + cfg schedule data via the dual-source pattern.
    private readonly IServiceConfigurationProvider<ScheduleConfiguration> _scheduleProvider;
    private readonly ILogger<GetScheduleEndpoint> _logger;

    public GetScheduleEndpoint(
        IServiceConfigurationProvider<ScheduleConfiguration> scheduleProvider,
        ILogger<GetScheduleEndpoint> logger)
    {
        _scheduleProvider = scheduleProvider;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("schedules/{Name}");
        // Why: Scheduler endpoints are anonymous — authentication is enforced at the service boundary (bearer token from callers).
        Policies("schedules:read");
        Summary(s =>
        {
            s.Summary = "Get a schedule by name";
            s.Description = "Retrieves a specific schedule by its unique name.";
        });
    }

    public override async Task HandleAsync(GetScheduleRequest req, CancellationToken ct)
    {
        SchedulerServerLog.GetScheduleRequestReceived(_logger, req.Name);

        var result = await _scheduleProvider.Get(req.Name, ct).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(ScheduleMapping.ToScheduleInfo(result.Value), ct).ConfigureAwait(false);
    }
}
