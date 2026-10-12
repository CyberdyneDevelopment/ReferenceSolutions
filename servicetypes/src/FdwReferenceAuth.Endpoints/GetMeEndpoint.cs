using Fdw.Services.Authorization.Configuration;
using System.Collections.Generic;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization;
using Fdw.Services.Users;
using Fdw.Services.Users.Configuration;
using Fdw.Services.Users.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint to get the current authenticated user's information.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetMeEndpoint : EndpointWithoutRequest<GetMeResponse>
{
    private readonly UserConfigurationProvider _userProvider;
    private readonly UserRoleConfigurationProvider _userRoleProvider;
    private readonly IRoleConfigurationProvider _roleProvider;
    private readonly UserTenantConfigurationProvider _userTenantProvider;
    private readonly IAuthenticationContextAccessor _authContextAccessor;
    private readonly ILogger<GetMeEndpoint> _logger;

    /// <inheritdoc />
    public GetMeEndpoint(
        UserConfigurationProvider userProvider,
        UserRoleConfigurationProvider userRoleProvider,
        IRoleConfigurationProvider roleProvider,
        UserTenantConfigurationProvider userTenantProvider,
        IAuthenticationContextAccessor authContextAccessor,
        ILogger<GetMeEndpoint>? logger = null)
    {
        _userProvider = userProvider;
        _userRoleProvider = userRoleProvider;
        _roleProvider = roleProvider;
        _userTenantProvider = userTenantProvider;
        _authContextAccessor = authContextAccessor;
        _logger = logger ?? NullLogger<GetMeEndpoint>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/users/me");
        // Why: API-60 — even "self" reads require a permission. A user with literally zero
        // permissions cannot read its own profile (a no-perms account is non-functional).
        Policies("users:read");
        Summary(s =>
        {
            s.Summary = "Get current user";
            s.Description = "Returns information about the currently authenticated user including their roles and tenant access.";
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        // Why: OpenIddict bakes the user's identity into the JWT 'sub' claim as the user's GUID Id,
        // not a username, and emits no name claim — so User.Identity.Name is always null here and the
        // old GetUser(username) lookup could never resolve the caller (every /me, including admin's,
        // returned 401/NotFound). Resolve the subject GUID from the standard identifier claim
        // (ClaimTypes.NameIdentifier maps from 'sub'; the raw 'sub' is read as a fallback claim name,
        // not a fallback value, when inbound claim mapping is disabled) and look the user up by Id.
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(subject) || !Guid.TryParse(subject, out var userId))
        {
            await Send.UnauthorizedAsync(ct).ConfigureAwait(false);
            return;
        }

        var userResult = await _userProvider.Get(userId, ct).ConfigureAwait(false);
        if (!userResult.IsSuccess)
        {
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        if (userResult.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var user = userResult.Value;

        // Which rows are this user's is UserRoleAssignments' to say (FDW-532: parsed Guids, not text,
        // because UserId is VARCHAR and SQL Server casts a uniqueidentifier to uppercase). A row for a
        // service identity ('fdw.scheduler') is another identity's assignment, not a malformed one:
        // it does not match a user and is not reported (FDW-991).
        var assignments = await _userRoleProvider
            .Find<IUserRoleImplementationConfiguration>(UserRoleAssignments.HeldBy(userId), ct)
            .ConfigureAwait(false);
        if (!assignments.IsSuccess || assignments.Value is null)
        {
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        // Each assignment names its role by the role's domain id; the Role domain reads that row and
        // hands back the implementation with the role's name stamped on it.
        var roleNames = new List<string>(assignments.Value.Count);
        foreach (var assignment in assignments.Value)
        {
            // A missing or unreadable role is a broken assignment, not an empty answer, so both a
            // failed read and a genuinely absent row are reported rather than skipped.
            var roleResult = await _roleProvider.Get(assignment.RoleId, ct).ConfigureAwait(false);
            if (!roleResult.IsSuccess || roleResult.Value is null)
            {
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            roleNames.Add(roleResult.Value.Name);
        }

        // TenantId/Permissions come from the caller's own established authentication context --
        // the same claims baked into the token at login -- not a second DB round-trip.
        var authContext = _authContextAccessor.Current;

        var membershipsResult = await _userTenantProvider
            .Find<IUserTenantImplementationConfiguration>(t => t.UserId == userId, ct)
            .ConfigureAwait(false);
        if (!membershipsResult.IsSuccess || membershipsResult.Value is null)
        {
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        var availableTenants = membershipsResult.Value.Select(m => m.TenantId).ToList();

        await Send.OkAsync(new GetMeResponse
        {
            Id = userId,
            Username = user.Name,
            Email = user.Email,
            Roles = roleNames,
            Permissions = authContext?.Permissions.ToList() ?? [],
            TenantId = authContext?.ActiveTenantId,
            AvailableTenants = availableTenants,
            // The same rule the server authorises platform-admin actions by (FDW-903): a session
            // console has no token to read globalTenant from, so it is told.
            IsPlatformAdmin = Resolve<ISystemRoleConfiguration>().IsPlatformAdmin(User),
        }, ct).ConfigureAwait(false);
    }
}
