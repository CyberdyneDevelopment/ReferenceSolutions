using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Sessions;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;

namespace FdwReferenceAuth.Endpoints;

/// <summary>Lists the roles the caller may act with (GET /sessions/act/roles, FDW-2282).</summary>
[ExcludeFromCodeCoverage]
public sealed class ListActingRolesEndpoint : EndpointWithoutRequest<ActingRolesResponse>
{
    private readonly ISessionActing _acting;

    /// <summary>Initializes a new instance of the <see cref="ListActingRolesEndpoint"/> class.</summary>
    /// <param name="acting">Works out the roles.</param>
    public ListActingRolesEndpoint(ISessionActing acting)
    {
        _acting = acting ?? throw new ArgumentNullException(nameof(acting));
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/sessions/act/roles");
        Summary(s =>
        {
            s.Summary = "List the roles to act with";
            s.Description = "The roles of the company the caller acts in that carry nothing the caller does not hold. The platform administrator is offered every role of the company.";
        });
        Tags("Sessions");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        var roles = await _acting.Roles(HttpContext, ct).ConfigureAwait(false);
        if (roles.IsFailure)
        {
            var (statusCode, problem) = ResultHttpStatusMapper.Map(roles, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: statusCode)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(new ActingRolesResponse { Roles = roles.Value! }, ct).ConfigureAwait(false);
    }
}
