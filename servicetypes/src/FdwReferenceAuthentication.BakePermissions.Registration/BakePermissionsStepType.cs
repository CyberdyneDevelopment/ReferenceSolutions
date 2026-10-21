using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Abstractions;
using Fdw.Claims.Abstractions;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Flow;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceAuthentication.BakePermissions.Registration;

/// <summary>
/// Resolves what the principal may do and states it on the context, for the token to carry.
/// </summary>
/// <remarks>
/// The option IS the step: a flow names it, the collection answers by that name, and what answers
/// is the thing that runs.
/// </remarks>
[ExcludeFromCodeCoverage]
[Implementation(typeof(AuthenticationStepTypes), "BakePermissions")]
public sealed class BakePermissionsStepType
    : AuthenticationStepTypeBase<IGenericService, IServiceFactory<IGenericService, IServiceConfiguration>>,
      IAuthenticationStep
{
    // Captured when the host is built: an option is created by its module initializer, which needs
    // a parameterless constructor, so what it needs arrives where a live container exists.
    // IUserPermissions and IAuthenticationContextAccessor are both Singleton, so
    // capturing them once here and holding them for the process's life is correct. ITenantProvider
    // is Scoped, so it is NOT captured here -- it arrives as an Execute parameter instead, supplied
    // fresh per call by AuthenticationRunner, which is itself Scoped.
    private IUserPermissions? _permissions;
    private IAuthenticationContextAccessor? _authContextAccessor;
    private ILogger _logger = NullLogger<BakePermissionsStepType>.Instance;

    /// <summary>Initializes a new instance of the <see cref="BakePermissionsStepType"/> class.</summary>
    public BakePermissionsStepType()
        : base("BakePermissions",
               "AuthenticationSteps",
               "Bake Permissions",
               "Resolves the principal's effective permissions and states them for the token to carry")
    {
        Initialization((host, loggerFactory) =>
        {
            _authContextAccessor = host.Services.GetRequiredService<IAuthenticationContextAccessor>();
            _permissions = host.Services.GetRequiredService<IUserPermissions>();
            _logger = loggerFactory?.CreateLogger<BakePermissionsStepType>()
                ?? NullLogger<BakePermissionsStepType>.Instance;

            return GenericResult<IHost>.Success(host);
        });
    }

    /// <inheritdoc />
    public IReadOnlyList<IContextElement> Requires => [ContextElements.Principal];

    /// <inheritdoc />
    public IReadOnlyList<IContextElement> Contributes => [ContextElements.Claims];

    /// <inheritdoc />
    /// <remarks>Gathering entitlements proves nothing about who the caller is.</remarks>
    public IReadOnlyList<string> AuthenticationMethods => [];

    /// <inheritdoc />
    public async Task<IGenericResult<StepOutcome>> Execute(
        AuthenticationContext context, ITenantProvider tenants, CancellationToken cancellationToken = default)
    {
        // Baking nothing would hand out a token carrying no permissions, which reads downstream as
        // a caller who may do nothing rather than as a step that never ran.
        if (_permissions is null || _authContextAccessor is null)
            return GenericResult<StepOutcome>.Failure(PermissionBakingLog.NotInitialized(_logger, Name));

        // Only this trusted built-in authentication operation runs under system context.
        using var systemScope = new SystemAuthenticationContextScope(_authContextAccessor);

        var principal = context.Principal!;

        // Every token is issued FOR a tenant, and the tenant must exist: it decides which roles count
        // and whether the token acts as the platform.
        var tenantResult = await tenants
            .GetTenant(principal.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (tenantResult.IsFailure)
            return tenantResult.ToNewResult<StepOutcome>();
        if (tenantResult.Value is null)
            return GenericResult<StepOutcome>.Failure(PermissionBakingLog.TenantMissing(_logger, principal.TenantId));

        var resolved = await ResolveEffective(_permissions, principal, tenantResult.Value, tenants, cancellationToken)
            .ConfigureAwait(false);
        if (resolved.IsFailure || resolved.Value is not { } effective)
            return resolved.ToNewResult<StepOutcome>();

        var permissions = effective.Permissions;
        var roleNames = effective.RoleNames;

        // Why Local and not Derived: these are read from this platform's own authorization tables.
        // Derived is for what the runner works out itself, such as an assurance level.
        var claims = permissions
            .Select(permission => new Claim
            {
                Type = ClaimDefinitions.perm.Name,
                Value = permission,
                Source = ClaimSources.Local,
            })
            .ToList();

        // Same tier the permissions above were resolved at -- the JwtBearerAuthenticationHandler's
        // RoleClaimType is wired to this claim, which is what makes ClaimsPrincipal.IsInRole/
        // ISystemRoleConfiguration.IsInRole able to answer for the 8 call sites that ask.
        claims.AddRange(roleNames.Select(roleName => new Claim
        {
            Type = ClaimDefinitions.roles.Name,
            Value = roleName,
            Source = ClaimSources.Local,
        }));

        // Stated only when the token is for a department (FDW-1802); the request pipeline resolves the department from it.
        if (principal.OrganizationId is { } organizationId)
        {
            claims.Add(new Claim
            {
                Type = ClaimDefinitions.orgId.Name,
                Value = organizationId.ToString(),
                Source = ClaimSources.Local,
            });
        }

        // Stated only for the global tenant, and read by ISystemRoleConfiguration.IsPlatformAdmin: an
        // Admin acting in the global tenant administers the platform, an Admin acting in any other
        // tenant administers only that tenant (FDW-825).
        if (tenantResult.Value.IsGlobal)
        {
            claims.Add(new Claim
            {
                Type = ClaimDefinitions.globalTenant.Name,
                Value = "true",
                Source = ClaimSources.Local,
            });
        }

        PermissionBakingLog.Baked(_logger, principal.Id, permissions.Count);
        PermissionBakingLog.BakedDetail(
            _logger, principal.Id, string.Join(", ", permissions), string.Join(", ", roleNames));

        return GenericResult<StepOutcome>.Success(
            new StepOutcome.Contributed(new ContextContribution
            {
                Claims = claims,

                // The roles behind these claims stop counting when their assignments expire, so the
                // token minted from them must not outlive the first of those (FDW-862).
                ClaimsValidUntil = effective.ValidUntil,
            }));
    }

    // The roles a token carries (FDW-825): the roles held in the tenant it is issued for -- see
    // IUserPermissions -- and, for a member of the global tenant acting as another
    // tenant, also the roles it holds in the global tenant. Acting in the global tenant a global
    // member sees and acts everywhere; switched into AFC it emulates a user holding those roles IN
    // AFC: RLS scopes it to AFC's rows, and the token carries no globalTenant claim, so it is not
    // the platform admin there.
    private async Task<IGenericResult<EffectiveAuthorization>> ResolveEffective(
        IUserPermissions userPermissions, Principal principal, ITenant tenant,
        ITenantProvider tenants, CancellationToken cancellationToken)
    {
        var userId = principal.Id.ToString();
        // The department the token is for, when it states one (FDW-1802): the permission service adds the person's access in it. The global
        // tenant's roles below are tenant-wide and take none.
        var inTenant = await userPermissions.Get(userId, tenant.Id, principal.OrganizationId, cancellationToken).ConfigureAwait(false);
        if (inTenant.IsFailure)
            return inTenant;
        if (inTenant.Value is null)
            return GenericResult<EffectiveAuthorization>.Failure(PermissionBakingLog.ResolvedNothing(_logger, principal.Id, tenant.Id));
        if (tenant.IsGlobal)
            return inTenant;

        var all = await tenants.GetAllTenants(cancellationToken).ConfigureAwait(false);
        if (all.IsFailure)
            return all.ToNewResult<EffectiveAuthorization>();
        if (all.Value is null || all.Value.FirstOrDefault(t => t.IsGlobal) is not { } global)
            return GenericResult<EffectiveAuthorization>.Failure(PermissionBakingLog.GlobalTenantMissing(_logger));

        // Membership of the global tenant is the same question the switch and issuance ask of it.
        var isGlobalMember = await tenants.ValidateTenantAccess(global.Id, userId, cancellationToken).ConfigureAwait(false);
        if (isGlobalMember.IsFailure)
            return isGlobalMember.ToNewResult<EffectiveAuthorization>();
        if (!isGlobalMember.Value)
            return inTenant;

        var inGlobal = await userPermissions.Get(userId, global.Id, orgId: null, cancellationToken).ConfigureAwait(false);
        if (inGlobal.IsFailure)
            return inGlobal;
        if (inGlobal.Value is null)
            return GenericResult<EffectiveAuthorization>.Failure(PermissionBakingLog.ResolvedNothing(_logger, principal.Id, global.Id));

        PermissionBakingLog.GlobalRolesApplied(_logger, principal.Id, tenant.Id, string.Join(", ", inGlobal.Value.RoleNames));
        return GenericResult<EffectiveAuthorization>.Success(new EffectiveAuthorization(
            inTenant.Value.Permissions.Union(inGlobal.Value.Permissions, StringComparer.OrdinalIgnoreCase).ToList(),
            inTenant.Value.RoleNames.Union(inGlobal.Value.RoleNames, StringComparer.OrdinalIgnoreCase).ToList(),
            Earliest(inTenant.Value.ValidUntil, inGlobal.Value.ValidUntil)));
    }

    // The union is true only while both halves are: the earlier end, where an absent end (null) is no end.
    private static DateTimeOffset? Earliest(DateTimeOffset? first, DateTimeOffset? second)
        => first is null || (second is not null && second < first) ? second : first;
}
