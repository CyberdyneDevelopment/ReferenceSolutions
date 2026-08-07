using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Reference.Etl.Server.Endpoints.Executions;

/// <summary>
/// Request for the resume endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ResumeExecutionRequest
{
    public Guid Id { get; set; }
}

/// <summary>
/// <c>POST /etl/executions/{id}/resume</c> — intentional 405 stub.
///
/// Resume of failed project executions from a checkpoint is not supported in v1.
/// The <c>AllowResume</c> policy field exists for future support.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ResumeExecutionEndpoint : Endpoint<ResumeExecutionRequest>
{
    private readonly ILogger<ResumeExecutionEndpoint> _logger;

    public ResumeExecutionEndpoint(ILogger<ResumeExecutionEndpoint> logger)
    {
        // Why NullLogger fallback: per FDW convention, ensures the endpoint remains functional
        // if DI does not wire up logging.
        _logger = logger ?? NullLogger<ResumeExecutionEndpoint>.Instance;
    }

    public override void Configure()
    {
        Post("etl/executions/{id}/resume");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:execute");
#endif
        Summary(s =>
        {
            s.Summary = "Resume an execution (not supported in v1)";
            s.Description = "Resume of failed project executions is not supported in v1. " +
                             "Returns 405 Method Not Allowed. The AllowResume policy field is reserved for future use.";
        });
    }

    public override async Task HandleAsync(ResumeExecutionRequest req, CancellationToken ct)
    {
        // Why: intentional 405 stub per plan decision. Resume requires checkpoint infrastructure
        // not yet implemented. AllowResume policy exists but has no runtime backing in v1.
        // Why: use Send.ResponseAsync to emit 405 — FastEndpoints intercepts bare StatusCode
        // assignments and replaces them with 204 when no body is written. The text body here
        // prevents that override.
        HttpContext.Response.Headers["Allow"] = "OPTIONS";
        await Send.ResponseAsync("Method Not Allowed: resume is not supported in v1", 405, ct).ConfigureAwait(false);
    }
}
