using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Etl.Abstractions.Execution;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Reference.Etl.Server.Endpoints.Executions;

/// <summary>
/// Request for the step endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class StepTestExecutionRequest
{
    public Guid Id { get; set; }
}

/// <summary>
/// <c>POST /etl/executions/{id}/step</c> — advance one batch in a paused test-mode execution.
///
/// Releases the pause flag for exactly one source-extract batch, then re-pauses automatically.
/// Very useful for inspecting exactly what each batch does to records.
/// Only valid for registered test-mode executions.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class StepExecutionEndpoint : Endpoint<StepTestExecutionRequest>
{
    private readonly IPipelineTestController _testController;
    private readonly ILogger<StepExecutionEndpoint> _logger;

    public StepExecutionEndpoint(IPipelineTestController testController, ILogger<StepExecutionEndpoint> logger)
    {
        _testController = testController;
        // Why NullLogger fallback: per FDW convention, ensures the endpoint remains functional
        // if DI does not wire up logging.
        _logger = logger ?? NullLogger<StepExecutionEndpoint>.Instance;
    }

    public override void Configure()
    {
        Post("etl/executions/{id}/step");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:execute");
#endif
        Summary(s =>
        {
            s.Summary = "Step one batch in a test-mode execution";
            s.Description = "Advances exactly one batch then re-pauses. Use for step-through inspection.";
        });
    }

    public override async Task HandleAsync(StepTestExecutionRequest req, CancellationToken ct)
    {
        var state = _testController.GetState(req.Id);
        if (state is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        _testController.Step(req.Id);
        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
