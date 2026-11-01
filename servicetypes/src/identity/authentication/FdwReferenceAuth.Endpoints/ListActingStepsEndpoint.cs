using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Sessions;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;

namespace FdwReferenceAuth.Endpoints;

/// <summary>Lists the sessions the caller has stepped through by acting with roles (GET /sessions/acting, FDW-2282).</summary>
[ExcludeFromCodeCoverage]
public sealed class ListActingStepsEndpoint : EndpointWithoutRequest<ActingStepsResponse>
{
    private readonly ISessionActing _acting;

    /// <summary>Initializes a new instance of the <see cref="ListActingStepsEndpoint"/> class.</summary>
    /// <param name="acting">Reads the chain.</param>
    public ListActingStepsEndpoint(ISessionActing acting)
    {
        _acting = acting ?? throw new ArgumentNullException(nameof(acting));
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/sessions/acting");
        Summary(s =>
        {
            s.Summary = "List the steps of acting";
            s.Description = "The caller's own session as step 0, then each role they have acted with in turn, the last being the session in hand. A caller who is not acting has just step 0.";
        });
        Tags("Sessions");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        var steps = await _acting.Steps(HttpContext, ct).ConfigureAwait(false);
        if (steps.IsFailure)
        {
            var (statusCode, problem) = ResultHttpStatusMapper.Map(steps, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: statusCode)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(
            new ActingStepsResponse
            {
                Steps = [.. steps.Value!.Select(step => new ActingStepDto { Step = step.Step, Role = step.Role, StartedAt = step.StartedAt, IsCurrent = step.IsCurrent })],
            },
            ct).ConfigureAwait(false);
    }
}
