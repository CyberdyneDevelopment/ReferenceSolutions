using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Sessions;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Stops acting with a role: ends the acting sessions above a step and wakes that step (POST /sessions/return, FDW-2282).
/// </summary>
/// <remarks>
/// The woken session has a new id, set as the cookie on this response, and its CSRF token is cleared: the browser app fetches a new one
/// from the usual endpoint. A caller who is not acting is refused with a message that says so.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class ReturnFromActingEndpoint : Endpoint<ReturnFromActingRequest>
{
    private readonly ISessionActing _acting;

    /// <summary>Initializes a new instance of the <see cref="ReturnFromActingEndpoint"/> class.</summary>
    /// <param name="acting">Returns from acting.</param>
    public ReturnFromActingEndpoint(ISessionActing acting)
    {
        _acting = acting ?? throw new ArgumentNullException(nameof(acting));
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/sessions/return");
        Summary(s =>
        {
            s.Summary = "Stop acting with a role";
            s.Description = "Ends the acting sessions above the named step (0 is your own session) and wakes that step, without signing in again. Refused when the caller is not acting or the step is not one below the session in hand.";
        });
        Tags("Sessions");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(ReturnFromActingRequest req, CancellationToken ct)
    {
        var returned = await _acting.Return(HttpContext, req.Step, ct).ConfigureAwait(false);
        if (returned.IsFailure)
        {
            var (statusCode, problem) = ResultHttpStatusMapper.Map(returned, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: statusCode)).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
