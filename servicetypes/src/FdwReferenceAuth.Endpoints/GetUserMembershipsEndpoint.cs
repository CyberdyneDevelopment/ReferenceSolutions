using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Users;
using Fdw.Services.Users.Configuration;
using Fdw.Services.Users.Endpoints;
using FdwReferenceAuth.Endpoints.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Answers "where does this user belong" for somebody else: every tenant the account is a member of,
/// and the roles it holds in each (FDW-1057).
/// </summary>
/// <remarks>
/// <para>
/// Who may ask is <see cref="AccountAuthority.Administers"/>, the same rule the other per-user routes
/// apply: the platform administrator, or the Admin role held in the tenant the account is homed in.
/// A caller who cannot see the account at all (row-level security hides accounts homed elsewhere)
/// gets 404, one who sees it but does not administer it gets 403.
/// </para>
/// <para>
/// The answer has the shape of <c>GET /me/memberships</c> and reads the same tables
/// (<c>tenant.UserTenantsConfiguration</c>, <c>authz.UserRoleConfiguration</c>). The caller's own
/// context shows one tenant's rows, and this answer spans every tenant the account belongs to, so the
/// reads run in the system context, each filtered to the account's id. <c>orgId</c> and
/// <c>orgName</c> are null on every item: a membership is tenant-wide (FDW-825) and no organization is
/// stored against a user.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class GetUserMembershipsEndpoint : Endpoint<UserScopedRequest, MembershipsResponse>
{
    private readonly UserConfigurationProvider _users;
    private readonly IUserTenantConfigurationProvider _memberships;
    private readonly IUserRoleConfigurationProvider _userRoles;
    private readonly IRoleConfigurationProvider _roles;
    private readonly ITenantProvider _tenants;
    private readonly IAuthenticationContextAccessor _authContextAccessor;
    private readonly ILogger<GetUserMembershipsEndpoint> _logger;

    /// <summary>Initializes a new instance of the <see cref="GetUserMembershipsEndpoint"/> class.</summary>
    /// <param name="users">Resolves the account from the route.</param>
    /// <param name="memberships">Supplies the account's tenant memberships.</param>
    /// <param name="userRoles">Supplies the account's role assignments, each held in one tenant.</param>
    /// <param name="roles">Resolves role names.</param>
    /// <param name="tenants">Resolves tenant display names.</param>
    /// <param name="authContextAccessor">Elevated while the account's rows in every tenant are read.</param>
    /// <param name="logger">The logger.</param>
    public GetUserMembershipsEndpoint(
        UserConfigurationProvider users,
        IUserTenantConfigurationProvider memberships,
        IUserRoleConfigurationProvider userRoles,
        IRoleConfigurationProvider roles,
        ITenantProvider tenants,
        IAuthenticationContextAccessor authContextAccessor,
        ILogger<GetUserMembershipsEndpoint>? logger)
    {
        _users = users;
        _memberships = memberships;
        _userRoles = userRoles;
        _roles = roles;
        _tenants = tenants;
        _authContextAccessor = authContextAccessor;
        _logger = logger ?? NullLogger<GetUserMembershipsEndpoint>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/users/{IdOrName}/memberships");
        Policies("users:read");
        Summary(s =>
        {
            s.Summary = "List an account's memberships";
            s.Description = "Every tenant the account is a member of, whether it is the default, and the roles held in each. "
                + "The caller must administer the account: the platform administrator, or an Admin of the tenant the account is homed in.";
        });
        Tags("Users");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(UserScopedRequest req, CancellationToken ct)
    {
        var account = await UserRouteTarget.Resolve(_users, User, req.IdOrName, ct).ConfigureAwait(false);

        // Row-level security hides accounts outside the caller's tenant, so "not found" also covers
        // "not yours to see".
        if (!account.IsSuccess || account.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        if (!AccountAuthority.Administers(User, account.Value, Resolve<ISystemRoleConfiguration>()))
        {
            TenantLog.UserMembershipsNotAdministered(_logger, req.IdOrName);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        TenantLog.ListingUserMemberships(_logger, req.IdOrName);

        var response = await Compose(account.Value.Id, ct).ConfigureAwait(false);
        if (response is null)
        {
            // A failed read is not an empty membership list: {items:[]} would say the account belongs nowhere.
            await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(response, ct).ConfigureAwait(false);
    }

    /// <summary>Reads the account's memberships, its role assignments, the role names and the tenant names, or null when any read fails.</summary>
    private async Task<MembershipsResponse?> Compose(Guid userId, CancellationToken ct)
    {
        // Why system context: RLS shows a caller the rows of the ONE tenant they are acting in, and this
        // answer spans every tenant the account belongs to. Every read below is filtered to the
        // account's own id, and only after AccountAuthority admitted the caller.
        using var systemScope = new SystemAuthenticationContextScope(_authContextAccessor);

        var memberships = await _memberships
            .Find<IUserTenantImplementationConfiguration>(m => m.UserId == userId, ct)
            .ConfigureAwait(false);

        // authz.UserRoleConfiguration.UserId is a Guid stored as VARCHAR; both sides are compared as the Guids they are (FDW-532).
        var held = await _userRoles
            .Find<IUserRoleImplementationConfiguration>(ur => Guid.TryParse(ur.UserId, out var assigned) && assigned == userId, ct)
            .ConfigureAwait(false);
        var roles = await _roles.Get(ct).ConfigureAwait(false);
        var tenants = await _tenants.GetAllTenants(ct).ConfigureAwait(false);

        if (!memberships.IsSuccess || memberships.Value is null
            || !held.IsSuccess || held.Value is null
            || !roles.IsSuccess || roles.Value is null
            || !tenants.IsSuccess || tenants.Value is null)
        {
            return null;
        }

        var byId = tenants.Value.ToDictionary(t => t.Id);
        var roleNames = roles.Value.ToDictionary(r => r.Id, r => r.Name);

        return new MembershipsResponse
        {
            Items = memberships.Value
                .Select(m => new MembershipDto
                {
                    TenantId = m.TenantId.ToString(),
                    TenantName = byId.TryGetValue(m.TenantId, out var t) ? t.DisplayName : null,
                    IsDefault = m.IsDefault,
                    IsSelectable = byId.TryGetValue(m.TenantId, out var s) && s.IsSelectable,
                    Roles = held.Value
                        .Where(ur => ur.TenantId == m.TenantId && roleNames.ContainsKey(ur.RoleId))
                        .Select(ur => roleNames[ur.RoleId])
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList(),
                })
                .ToList(),
        };
    }
}
