using Fdw.Claims.Abstractions;
using Fdw.Services.Authentication.Abstractions.Steps;
using Microsoft.AspNetCore.Http;

namespace FdwReferenceAuthentication.TenantSwitch.Registration;

/// <summary>
/// Reads a tenant-switch request out of the current HTTP request: the caller from the token the
/// request was authenticated with, the requested tenant from what the endpoint bound.
/// </summary>
public sealed class HttpTenantSwitchRequestAccessor : ITenantSwitchRequestAccessor
{
    private readonly IHttpContextAccessor _context;

    /// <summary>Initializes a new instance of the <see cref="HttpTenantSwitchRequestAccessor"/> class.</summary>
    /// <param name="context">Supplies the current request.</param>
    public HttpTenantSwitchRequestAccessor(IHttpContextAccessor context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc />
    /// <remarks>
    /// Only an authenticated request names a caller. The subject comes from the token this platform
    /// issued and authentication just validated, so a switch re-issues for the identity the caller
    /// already proved — it never accepts an identity from the request body.
    /// </remarks>
    public Guid? CallerId
    {
        get
        {
            var user = _context.HttpContext?.User;
            return user?.Identity?.IsAuthenticated == true
                && Guid.TryParse(user.FindFirst(ClaimDefinitions.sub.Name)?.Value, out var callerId)
                ? callerId
                : null;
        }
    }

    /// <inheritdoc />
    public Guid? RequestedTenantId
        => _context.HttpContext?.Items.TryGetValue(RequestedTenantItemKey, out var bound) == true
            ? bound as Guid?
            : null;

    /// <inheritdoc />
    public Guid? RequestedOrganizationId
        => _context.HttpContext?.Items.TryGetValue(RequestedOrganizationItemKey, out var bound) == true
            ? bound as Guid?
            : null;

    /// <summary>
    /// The key an endpoint stores the bound requested department (organization) under, when the switch asks for one (FDW-1802).
    /// </summary>
    public const string RequestedOrganizationItemKey = "Fdw.Authentication.TenantSwitch.OrganizationId";

    /// <summary>
    /// The key an endpoint stores the bound requested tenant under. The model binder has already
    /// read a JSON body by the time a step runs, and the stream is not rewindable.
    /// </summary>
    public const string RequestedTenantItemKey = "Fdw.Authentication.TenantSwitch.TenantId";
}
