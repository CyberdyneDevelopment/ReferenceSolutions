using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Scheduling.Abstractions;
using Microsoft.Extensions.Logging;
using Reference.Scheduler.Server.Logging;

namespace Reference.Scheduler.Server.Endpoints;

[ExcludeFromCodeCoverage]
public sealed class DeleteScheduleRequest
{
    public string Name { get; set; } = string.Empty;
}

[ExcludeFromCodeCoverage]
public sealed class DeleteScheduleEndpoint : Endpoint<DeleteScheduleRequest>
{
    private readonly IFrameworkSchedulingService _schedulingService;
    private readonly ILogger<DeleteScheduleEndpoint> _logger;

    public DeleteScheduleEndpoint(IFrameworkSchedulingService schedulingService, ILogger<DeleteScheduleEndpoint> logger)
    {
        _schedulingService = schedulingService;
        _logger = logger;
    }

    public override void Configure()
    {
        Delete("schedules/{Name}");
        // Why: Scheduler endpoints are anonymous — authentication is enforced at the service boundary (bearer token from callers).
        Policies("schedules:write");
        Summary(s =>
        {
            s.Summary = "Delete a schedule";
            s.Description = "Deletes an existing schedule by name.";
        });
    }

    public override async Task HandleAsync(DeleteScheduleRequest req, CancellationToken ct)
    {
        SchedulerServerLog.DeleteScheduleRequestReceived(_logger, req.Name);

        var result = await _schedulingService.DeleteSchedule(req.Name, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            AddError(SchedulerServerLog.GetError(result, _logger));
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
