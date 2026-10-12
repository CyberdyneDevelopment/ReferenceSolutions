using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Users;
using Fdw.Services.Users.Configuration;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Answers "where does this user belong" — every tenant the caller is a member of, and the roles
/// they hold in each.
/// </summary>
/// <remarks>
/// The console cannot switch tenants without this: the tenant is the answer, not the input.
/// Membership is <c>tenant.UserTenantsConfiguration</c> — the same table login, the tenant switch
/// and RLS consult — and roles are <c>authz.UserRoleConfiguration</c>, held per tenant (FDW-825).
/// <c>tenant.TenantOrgAccess</c> is org-level visibility narrowing, not membership, and is not read.
/// </remarks>
[ExcludeFromCodeCoverage]
public class GetMyMembershipsEndpoint : EndpointWithoutRequest<MembershipsResponse>
{
    private readonly IUserTenantConfigurationProvider _memberships;
    private readonly IUserRoleConfigurationProvider _userRoles;
    private readonly IRoleConfigurationProvider _roles;
    private readonly ITenantProvider _tenants;
    private readonly IAuthenticationContextAccessor _authContextAccessor;

    /// <summary>Initializes a new instance of the <see cref="GetMyMembershipsEndpoint"/> class.</summary>
    /// <param name="memberships">Supplies the caller's tenant memberships.</param>
    /// <param name="userRoles">Supplies the caller's role assignments, each held in one tenant.</param>
    /// <param name="roles">Resolves role names.</param>
    /// <param name="tenants">Resolves tenant display names.</param>
    /// <param name="authContextAccessor">Elevated while the caller's own rows in every tenant are read.</param>
    public GetMyMembershipsEndpoint(
        IUserTenantConfigurationProvider memberships,
        IUserRoleConfigurationProvider userRoles,
        IRoleConfigurationProvider roles,
        ITenantProvider tenants,
        IAuthenticationContextAccessor authContextAccessor)
    {
        _memberships = memberships;
        _userRoles = userRoles;
        _roles = roles;
        _tenants = tenants;
        _authContextAccessor = authContextAccessor;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/me/memberships");
        // Reading your own memberships needs no permission beyond being signed in: a user with
        // no grants anywhere still has to be told that, and gating this on a permission would
        // make an empty answer indistinguishable from a refused one.
        Summary(s =>
        {
            s.Summary = "List my memberships";
            s.Description = "Every tenant the calling user is a member of, with the roles they hold in each.";
        });
        Tags("Auth");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        // The subject GUID, same as GetMeEndpoint: no name claim is emitted, so User.Identity.Name
        // is null here and only the identifier claim can resolve the caller.
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(subject) || !Guid.TryParse(subject, out var userId))
        {
            await Send.UnauthorizedAsync(ct).ConfigureAwait(false);
            return;
        }

        // Why system context: RLS shows a caller the rows of the ONE tenant they are acting in, and
        // this answer spans every tenant they belong to. Every read below is filtered to the
        // caller's own id, so elevating reads nobody else's rows -- the same platform lookup login
        // makes when it picks the tenant a sign-in lands in.
        using var systemScope = new SystemAuthenticationContextScope(_authContextAccessor);

        var memberships = await _memberships
            .Find<IUserTenantImplementationConfiguration>(m => m.UserId == userId, ct)
            .ConfigureAwait(false);
        // A failed read is not an empty membership list. Answering 200 with {items:[]} would tell
        // the caller they belong nowhere, which is a different fact from "we could not look".
        if (!memberships.IsSuccess || memberships.Value is null)
        {
            await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
            return;
        }

        // authz.UserRoleConfiguration.UserId is a Guid stored as VARCHAR; both sides are compared as
        // the Guids they are (FDW-532).
        var held = await _userRoles
            .Find<IUserRoleImplementationConfiguration>(ur => Guid.TryParse(ur.UserId, out var assigned) && assigned == userId, ct)
            .ConfigureAwait(false);
        var roles = await _roles.Get(ct).ConfigureAwait(false);
        // Roles are part of the membership, not decoration: a membership listed with no roles when
        // the roles could not be read would say "a member holding nothing", which is a real and
        // different state.
        if (!held.IsSuccess || held.Value is null || !roles.IsSuccess || roles.Value is null)
        {
            await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
            return;
        }

        var tenants = await _tenants.GetAllTenants(ct).ConfigureAwait(false);
        // Selectable is part of the membership the console acts on -- which tenants its picker offers
        // -- so unlike the names it cannot be decoration: a failed tenant read fails the answer.
        if (!tenants.IsSuccess || tenants.Value is null)
        {
            await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
            return;
        }

        var byId = tenants.Value.ToDictionary(t => t.Id);
        var roleNames = roles.Value.ToDictionary(r => r.Id, r => r.Name);

        await Send.OkAsync(new MembershipsResponse
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
        }, ct).ConfigureAwait(false);
    }
}
