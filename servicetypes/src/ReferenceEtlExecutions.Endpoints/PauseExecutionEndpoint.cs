using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Etl.Abstractions.Execution;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ReferenceEtlExecutions.Endpoints;

/// <summary>
/// Request for the pause endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class PauseExecutionRequest
{
    public Guid Id { get; set; }
}

/// <summary>
/// <c>POST /etl/executions/{id}/pause</c> — pause a test-mode pipeline execution.
///
/// Sets the pause flag so the pipeline's batch loop waits before processing the next batch.
/// Only valid for test-mode executions. Production executions are not pauseable.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class PauseExecutionEndpoint : Endpoint<PauseExecutionRequest>
{
    private readonly IPipelineTestController _testController;
    private readonly ILogger<PauseExecutionEndpoint> _logger;

    public PauseExecutionEndpoint(IPipelineTestController testController, ILogger<PauseExecutionEndpoint> logger)
    {
        _testController = testController;
        // Why NullLogger fallback: per FDW convention, ensures the endpoint remains functional
        // if DI does not wire up logging.
        _logger = logger ?? NullLogger<PauseExecutionEndpoint>.Instance;
    }

    public override void Configure()
    {
        Post("etl/executions/{id}/pause");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:execute");
#endif
        Summary(s =>
        {
            s.Summary = "Pause a test-mode execution";
            s.Description = "Sets the pause flag for a test-mode execution. The pipeline will halt before the next batch.";
        });
    }

    public override async Task HandleAsync(PauseExecutionRequest req, CancellationToken ct)
    {
        var state = _testController.GetState(req.Id);
        if (state is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        _testController.Pause(req.Id);
        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
