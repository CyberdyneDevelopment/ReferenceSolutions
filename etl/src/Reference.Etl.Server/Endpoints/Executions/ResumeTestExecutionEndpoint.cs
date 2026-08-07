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
/// Request for test-mode resume.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ResumeTestExecutionRequest
{
    public Guid Id { get; set; }
}

/// <summary>
/// <c>POST /etl/executions/{id}/test-resume</c> — resume a paused test-mode execution.
///
/// Releases the pause flag so the pipeline continues processing from where it paused.
/// Only valid for registered test-mode executions.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ResumeTestExecutionEndpoint : Endpoint<ResumeTestExecutionRequest>
{
    private readonly IPipelineTestController _testController;
    private readonly ILogger<ResumeTestExecutionEndpoint> _logger;

    public ResumeTestExecutionEndpoint(IPipelineTestController testController, ILogger<ResumeTestExecutionEndpoint> logger)
    {
        _testController = testController;
        // Why NullLogger fallback: per FDW convention, ensures the endpoint remains functional
        // if DI does not wire up logging.
        _logger = logger ?? NullLogger<ResumeTestExecutionEndpoint>.Instance;
    }

    public override void Configure()
    {
        Post("etl/executions/{id}/test-resume");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:execute");
#endif
        Summary(s =>
        {
            s.Summary = "Resume a paused test-mode execution";
            s.Description = "Releases the pause flag so the pipeline continues from where it halted.";
        });
    }

    public override async Task HandleAsync(ResumeTestExecutionRequest req, CancellationToken ct)
    {
        var state = _testController.GetState(req.Id);
        if (state is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        _testController.Resume(req.Id);
        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
