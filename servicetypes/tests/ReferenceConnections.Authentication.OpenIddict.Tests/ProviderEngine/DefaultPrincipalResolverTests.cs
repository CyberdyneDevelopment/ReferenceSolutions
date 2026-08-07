using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Authentication;
using Fdw.Services.Authentication.Abstractions;
using ReferenceAuthentication.OpenIddict.Claims;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Users;
using Fdw.Services.Users.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.OpenIddict.Tests.ProviderEngine;

/// <summary>
/// Unit tests for <see cref="DefaultPrincipalResolver"/>.
/// Mocks IUserTenantService, IOrganizationProvider, IEffectivePermissionResolver.
/// Asserts: sub/tenant_id/org_id/role/perm claims are correctly baked;
/// missing tenant → fail-loud (non-success, no principal returned);
/// failed permission resolution → fail-loud.
/// </summary>
public sealed class DefaultPrincipalResolverTests
{
    private readonly Mock<IOrganizationProvider> _orgProviderMock = new(MockBehavior.Strict);
    private readonly Mock<IEffectivePermissionResolver> _permResolverMock = new(MockBehavior.Strict);
    // Why: DefaultPrincipalResolver now injects UserTenantConfigurationProvider (concrete) directly;
    // IUserTenantService was deleted. Mock the concrete provider — its methods are virtual.
    private readonly Mock<UserTenantConfigurationProvider> _tenantProviderMock = new(
        MockBehavior.Strict,
        NullLogger<UserTenantConfigurationProvider>.Instance,
        new Lazy<IConfigurationGateway>(() => null!),
        "ConfigurationDb",
        "tenant",
        // Why: Castle DynamicProxy ignores optional ctor params — pass trailing invalidator/dataStores
        // explicitly so the 7-param ctor arity matches. (object?)null! per repo convention.
        (object?)null!);

    // Why: UserRoleConfigurationProvider and RoleConfigurationProvider are injected for "roles" JWT baking.
    // Existing tests focus on tenant/org/permission logic; role providers return empty lists.
    private DefaultPrincipalResolver CreateSut()
    {
        var userRoleProviderMock = new Mock<UserRoleConfigurationProvider>(
            MockBehavior.Loose,
            NullLogger<UserRoleConfigurationProvider>.Instance,
            new Lazy<IConfigurationGateway>(() => null!),
            "ConfigurationDb",
            "authz",
            (object?)null!);

        // Why: GetByUser is virtual and is the only method LoadRoleNames calls on this provider.
        // Set it up to return empty so existing tenant/org/perm tests are unaffected.
        userRoleProviderMock
            .Setup(p => p.GetByUser(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<UserRoleConfiguration>>.Success(
                Array.Empty<UserRoleConfiguration>()));

        var roleProviderMock = new Mock<RoleConfigurationProvider>(
            MockBehavior.Loose,
            NullLogger<RoleConfigurationProvider>.Instance,
            new Lazy<IConfigurationGateway>(() => null!),
            "ConfigurationDb",
            "authz",
            (object?)null!);

        // Why: LoadRoleNames calls GetAllRoles to match assignment ids to names. Stub it empty so
        // these tenant/org/perm tests don't hit the real gateway (null here) and NRE.
        roleProviderMock
            .Setup(p => p.GetAllRoles(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RoleConfiguration>());

        return new DefaultPrincipalResolver(
            _tenantProviderMock.Object,
            _orgProviderMock.Object,
            _permResolverMock.Object,
            userRoleProviderMock.Object,
            roleProviderMock.Object,
            NullLogger<DefaultPrincipalResolver>.Instance);
    }

    [Fact]
    public async Task Resolve_DefaultTenant_BakesSubTenantOrgRolePerm()
    {
        // Why: null tenantId → resolver calls GetDefaultTenant(IsDefault=1).
        // This verifies the default-tenant path without specifying an explicit tenant.
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(s => s.GetDefaultTenant(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid?>.Success(tenantId));

        _orgProviderMock
            .Setup(o => o.GetDefault(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Success(
                new OrganizationConfiguration { Id = orgId, TenantId = tenantId }));

        _permResolverMock
            .Setup(p => p.Resolve(userId.ToString(), tenantId, orgId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyCollection<string>>.Success(
                new[] { "data.read", "data.write" }));

        var sut = CreateSut();
        var result = await sut.Resolve(userId, tenantId: null, orgId: null, additionalRoles: Array.Empty<string>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var principal = result.Value!;

        principal.FindFirst(ClaimDefinitions.sub.Name)?.Value.ShouldBe(userId.ToString());
        principal.FindFirst(ClaimDefinitions.tenantId.Name)?.Value.ShouldBe(tenantId.ToString());
        principal.FindFirst(ClaimDefinitions.orgId.Name)?.Value.ShouldBe(orgId.ToString());

        var perms = principal.FindAll(ClaimDefinitions.perm.Name).Select(c => c.Value).ToList();
        perms.ShouldContain("data.read");
        perms.ShouldContain("data.write");
    }

    [Fact]
    public async Task Resolve_ExplicitTenant_ValidatesMembership_ThenBakesClaims()
    {
        // Why: explicit tenantId → resolver calls GetUserTenants to validate membership.
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(s => s.GetUserTenants(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<Guid>>.Success(new[] { tenantId }));

        _orgProviderMock
            .Setup(o => o.GetDefault(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Success(
                new OrganizationConfiguration { Id = orgId, TenantId = tenantId }));

        _permResolverMock
            .Setup(p => p.Resolve(userId.ToString(), tenantId, orgId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyCollection<string>>.Success(
                new[] { "data.read" }));

        var sut = CreateSut();
        var result = await sut.Resolve(userId, tenantId: tenantId, orgId: null, additionalRoles: Array.Empty<string>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.FindFirst(ClaimDefinitions.tenantId.Name)?.Value.ShouldBe(tenantId.ToString());
    }

    [Fact]
    public async Task Resolve_ExplicitTenant_NotMember_FailsLoud()
    {
        // Why: explicit tenant the user does not belong to → TenantAccessDenied, no token.
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(s => s.GetUserTenants(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<Guid>>.Success(new[] { otherTenantId }));

        var sut = CreateSut();
        var result = await sut.Resolve(userId, tenantId: tenantId, orgId: null, additionalRoles: Array.Empty<string>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
    }

    [Fact]
    public async Task Resolve_AgentKeyPath_AddsAgentRole()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(s => s.GetDefaultTenant(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid?>.Success(tenantId));

        _orgProviderMock
            .Setup(o => o.GetDefault(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Failure(new GenericMessage("No default org")));

        _permResolverMock
            .Setup(p => p.Resolve(userId.ToString(), tenantId, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyCollection<string>>.Success(
                new[] { "data.read" }));

        var sut = CreateSut();
        var result = await sut.Resolve(userId, tenantId: null, orgId: null, additionalRoles: new[] { "agent" }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var roles = result.Value!.FindAll(ClaimDefinitions.roles.Name).Select(c => c.Value).ToList();
        roles.ShouldContain("agent");
    }

    [Fact]
    public async Task Resolve_NoTenants_FailsLoud()
    {
        var userId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(s => s.GetDefaultTenant(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid?>.Success((Guid?)null));

        var sut = CreateSut();
        var result = await sut.Resolve(userId, tenantId: null, orgId: null, additionalRoles: Array.Empty<string>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
    }

    [Fact]
    public async Task Resolve_TenantQueryFails_FailsLoud()
    {
        var userId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(s => s.GetDefaultTenant(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid?>.Failure(new GenericMessage("DB error")));

        var sut = CreateSut();
        var result = await sut.Resolve(userId, tenantId: null, orgId: null, additionalRoles: Array.Empty<string>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task Resolve_PermissionResolutionFails_FailsLoud()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(s => s.GetDefaultTenant(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid?>.Success(tenantId));

        _orgProviderMock
            .Setup(o => o.GetDefault(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Failure(new GenericMessage("No org")));

        _permResolverMock
            .Setup(p => p.Resolve(userId.ToString(), tenantId, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyCollection<string>>.Failure(new GenericMessage("Permission query failed")));

        var sut = CreateSut();
        var result = await sut.Resolve(userId, tenantId: null, orgId: null, additionalRoles: Array.Empty<string>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Resolve_CrossTenant_WithViewAllPerm_IssuesCrossTenantToken()
    {
        // Why: user holds tenants:view-all → cross_tenant claim baked, no tenant_id.
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(s => s.GetDefaultTenant(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid?>.Success(tenantId));

        _orgProviderMock
            .Setup(o => o.GetDefault(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Failure(new GenericMessage("No org")));

        _permResolverMock
            .Setup(p => p.Resolve(userId.ToString(), tenantId, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyCollection<string>>.Success(
                new[] { "data.read", "tenants:view-all" }));

        var sut = CreateSut();
        var result = await sut.Resolve(userId, tenantId: null, orgId: null, isCrossTenant: true, additionalRoles: Array.Empty<string>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var principal = result.Value!;
        principal.FindFirst(ClaimDefinitions.crossTenant.Name)?.Value.ShouldBe("true");
        principal.FindFirst(ClaimDefinitions.tenantId.Name).ShouldBeNull();
    }

    [Fact]
    public async Task Resolve_CrossTenant_WithoutViewAllPerm_FailsLoud()
    {
        // Why: cross-tenant requested but user lacks tenants:view-all → fail, no token.
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _tenantProviderMock
            .Setup(s => s.GetDefaultTenant(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid?>.Success(tenantId));

        _orgProviderMock
            .Setup(o => o.GetDefault(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Failure(new GenericMessage("No org")));

        _permResolverMock
            .Setup(p => p.Resolve(userId.ToString(), tenantId, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyCollection<string>>.Success(
                new[] { "data.read" }));

        var sut = CreateSut();
        var result = await sut.Resolve(userId, tenantId: null, orgId: null, isCrossTenant: true, additionalRoles: Array.Empty<string>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
    }

    [Fact]
    public async Task Resolve_CrossTenantAndExplicitTenant_FailsLoud()
    {
        // Why: mutually exclusive — cross-tenant and explicit tenant_id together is invalid.
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var sut = CreateSut();
        var result = await sut.Resolve(userId, tenantId: tenantId, orgId: null, isCrossTenant: true, additionalRoles: Array.Empty<string>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }
}
