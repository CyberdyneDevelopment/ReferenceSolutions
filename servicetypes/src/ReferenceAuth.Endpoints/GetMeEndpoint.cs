using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authorization;
using Fdw.Services.Users;
using Fdw.Services.Users.Endpoints;

namespace ReferenceAuth.Endpoints;

/// <summary>
/// Endpoint to get the current authenticated user's information.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetMeEndpoint : EndpointWithoutRequest<GetMeResponse>
{
    private readonly UserConfigurationProvider _userProvider;
    private readonly UserRoleConfigurationProvider _userRoleProvider;
    private readonly RoleConfigurationProvider _roleProvider;

    /// <inheritdoc />
    public GetMeEndpoint(
        UserConfigurationProvider userProvider,
        UserRoleConfigurationProvider userRoleProvider,
        RoleConfigurationProvider roleProvider)
    {
        _userProvider = userProvider;
        _userRoleProvider = userRoleProvider;
        _roleProvider = roleProvider;
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

        var userResult = await _userProvider.GetUser(userId, ct).ConfigureAwait(false);
        if (!userResult.IsSuccess || userResult.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var user = userResult.Value;

        // Why: UserRoleConfigurationProvider.GetByUser keys on the user ID string.
        var rolesResult = await _userRoleProvider.GetByUser(userId.ToString(), ct).ConfigureAwait(false);
        if (!rolesResult.IsSuccess || rolesResult.Value is null)
        {
            await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
            return;
        }

        // Why: UserRoleConfiguration.Name is the composite "userId:roleId" key, not the role display
        // name. Join against all roles to resolve the human-readable role names for the response.
        var allRoles = await _roleProvider.GetAllRoles(ct).ConfigureAwait(false);
        var roleNames = rolesResult.Value
            .Select(ur => allRoles.FirstOrDefault(r => r.Id == ur.RoleId)?.Name)
            .Where(name => name is not null)
            .ToList();

        await Send.OkAsync(new GetMeResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Roles = roleNames!
        }, ct).ConfigureAwait(false);
    }
}
