using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Web.RestEndpoints.ApiReference;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// The sign-in page's document: the auth routes, cut to what the caller may call —
/// <c>GET /openapi/sign-in</c>.
/// </summary>
/// <remarks>
/// A signed-out visitor sees only the auth routes that admit anonymous callers: signing in, the
/// redirect and redeem legs of an external sign-in, and changing a required password. Signed in, the
/// same page shows the auth routes the caller holds the permissions for.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class SignInReferenceDocumentEndpoint : ApiReferenceDocumentEndpointBase<EmptyRequest>
{
    /// <inheritdoc />
    public SignInReferenceDocumentEndpoint(
        ILogger<ApiReferenceDocumentEndpointBase<EmptyRequest>> logger,
        ISystemRoleConfiguration systemRoles)
        : base(logger, systemRoles)
    {
    }

    /// <inheritdoc />
    protected override string DocumentName => FdwReferenceEndpoints.Endpoints.OpenApiDocumentName;

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/openapi/sign-in");
        AllowAnonymous();
        Description(builder => builder.ExcludeFromDescription());
    }

    /// <inheritdoc />
    protected override Task<IGenericResult> Filter(
        ApiReferenceDocument document,
        ApiReferenceCaller caller,
        EmptyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        var authRoutes = "/" + FdwReferenceEndpoints.Endpoints.RoutePrefix + "/auth/";
        document.KeepPaths(path => path.StartsWith(authRoutes, StringComparison.OrdinalIgnoreCase), Logger);
        document.SetTitle("Sign in");
        return Task.FromResult<IGenericResult>(document.KeepAdmitted(caller, Logger));
    }
}
