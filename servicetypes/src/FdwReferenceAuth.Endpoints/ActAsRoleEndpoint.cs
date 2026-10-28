using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Sessions;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Starts a session that acts with one role, and puts the caller's own session to sleep (POST /sessions/act, FDW-2282).
/// </summary>
/// <remarks>
/// Open to any caller signed in by a session: whether the role may be acted with is the answer of <see cref="ISessionActing"/>, which refuses a
/// role the session does not cover, a role of another company, and a role that does not exist, each with the reason the caller is given. The
/// new session's cookie is set on the response; the browser app fetches a CSRF token for it from the usual endpoint.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class ActAsRoleEndpoint : Endpoint<ActAsRoleRequest>
{
    private readonly ISessionActing _acting;
    private readonly ILogger<ActAsRoleEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="ActAsRoleEndpoint"/> class.</summary>
    /// <param name="acting">Starts the acting session.</param>
    /// <param name="logger">The logger.</param>
    public ActAsRoleEndpoint(ISessionActing acting, ILogger<ActAsRoleEndpoint>? logger = null)
    {
        _acting = acting ?? throw new ArgumentNullException(nameof(acting));
        _logger = logger ?? NullLogger<ActAsRoleEndpoint>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/sessions/act");
        Summary(s =>
        {
            s.Summary = "Act with a role";
            s.Description = "Starts a session that holds only what the named role grants, and puts the caller's own session to sleep until they return. Refused when the role carries anything the caller does not hold, belongs to another company, or does not exist.";
        });
        Tags("Sessions");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(ActAsRoleRequest req, CancellationToken ct)
    {
        var acted = await _acting.Act(HttpContext, req.Role, ct).ConfigureAwait(false);
        if (acted.IsFailure)
        {
            var (statusCode, problem) = ResultHttpStatusMapper.Map(acted, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: statusCode)).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
