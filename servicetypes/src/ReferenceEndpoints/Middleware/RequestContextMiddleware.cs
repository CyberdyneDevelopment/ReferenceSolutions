using System;
using System.Linq;
using System.Threading.Tasks;
using ReferenceEndpoints.Extensions;
using ReferenceEndpoints.Logging;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ReferenceEndpoints.Middleware;

/// <summary>
/// Middleware that builds an <see cref="IRequestContext"/> from the authenticated
/// <see cref="Microsoft.AspNetCore.Http.HttpContext.User"/> and stores it in
/// <see cref="HttpContext.Items"/> for downstream use. Also establishes the ambient
/// <see cref="IAuthenticationContextAccessor.Current"/> for this request's logical call flow, so
/// tenant-scoped RLS (<c>security.fn_TenantFilter</c>) sees a real identity on every DataGateway
/// read/write reached from this request.
/// </summary>
// Why: Must run AFTER UseAuthentication() so that HttpContext.User is populated with
// validated JWT claims. If placed before authentication the ClaimsPrincipal is always
// anonymous and the context would always be GuestContext — defeating its purpose.
public sealed class RequestContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestContextMiddleware> _logger;
    private readonly IAuthenticationContextAccessor? _authContextAccessor;

    /// <summary>
    /// Initializes a new instance of <see cref="RequestContextMiddleware"/>.
    /// </summary>
    public RequestContextMiddleware(
        RequestDelegate next,
        ILogger<RequestContextMiddleware>? logger = null,
        IAuthenticationContextAccessor? authContextAccessor = null)
    {
        ArgumentNullException.ThrowIfNull(next);

        _next = next;
        _logger = logger ?? NullLogger<RequestContextMiddleware>.Instance;
        _authContextAccessor = authContextAccessor;
    }

    /// <summary>
    /// Invokes the middleware for the current request.
    /// </summary>
    public Task Invoke(HttpContext context)
    {
        context.SetRequestContext(BuildRequestContext(context));

        // Why: mirrors ASP.NET Core's own IHttpContextAccessor pattern — set once per request on
        // an AsyncLocal-backed singleton, visible to everything awaited from this point onward
        // (including MsSqlConnection.SetUserSessionContext deeper in the pipeline). An authenticated
        // request gets a real IAuthenticationContext so RLS SESSION_CONTEXT(UserId/TenantId/...) is
        // set; an anonymous request leaves Current null (fail closed) rather than defaulting to any
        // elevated visibility — there is no "anonymous therefore system" fallback.
        if (_authContextAccessor is not null)
        {
            _authContextAccessor.Current = context.User.Identity?.IsAuthenticated == true
                ? new ClaimsPrincipalAuthenticationContext(context.User)
                : null;
        }

        return _next(context);
    }

    private RequestContext BuildRequestContext(HttpContext context)
    {
        var user = context.User;

        if (user.Identity?.IsAuthenticated != true)
            return RequestContext.GuestContext;

        // Extract tenant ID from the "tenantId" JWT claim (same claim used by multitenancy middleware)
        var tenantClaim = user.FindFirst(ClaimDefinitions.tenantId.Name)?.Value;
        var tenantId = !string.IsNullOrEmpty(tenantClaim) && Guid.TryParse(tenantClaim, out var parsedTenant)
            ? parsedTenant
            : Guid.Empty;

        // Collect roles from both "role" (standard JWT) and ClaimTypes.Role claims, deduplicated
        // Why: JWT Bearer middleware maps inbound claims; depending on token shape, roles may
        // arrive under either claim type. Collecting both prevents missed roles.
        var roles = user.FindAll("role")
            .Concat(user.FindAll(System.Security.Claims.ClaimTypes.Role))
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        RequestContextLog.RequestContextBuilt(_logger, tenantId.ToString(), roles.Count);

        // OrganizationIds: empty — org membership not yet implemented in JWT claims
        return new RequestContext(tenantId, [], roles);
    }
}
