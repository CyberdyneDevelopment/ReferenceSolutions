using Fdw.Claims.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FdwReferenceAuthentication.BakePermissions.Registration.Tests;

/// <summary>
/// Tests for baking the permissions of a token for a department (FDW-1802): the department is passed to the permission service and
/// written into the token as the orgId claim; a token for no department carries none and asks the service for none.
/// </summary>
public sealed class BakePermissionsDepartmentTests
{
    private static readonly Guid Tenant = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");

    private static async Task<(StepOutcome.Contributed Outcome, Mock<IUserPermissions> Permissions, Guid UserId)> Bake(Guid? organizationId)
    {
        var userId = Guid.NewGuid();
        var tenant = new Mock<ITenant>();
        tenant.SetupGet(t => t.Id).Returns(Tenant);
        tenant.SetupGet(t => t.IsGlobal).Returns(true);
        var tenants = new Mock<ITenantProvider>();
        tenants.Setup(t => t.GetTenant(Tenant, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<ITenant>.Success(tenant.Object));
        var permissions = new Mock<IUserPermissions>();
        permissions.Setup(p => p.Get(userId.ToString(), Tenant, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<EffectiveAuthorization>.Success(new EffectiveAuthorization(["organizations:read"], ["Administrator"], null)));
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<IAuthenticationContextAccessor>(new AuthenticationContextAccessor());
        builder.Services.AddSingleton(permissions.Object);
        using var host = builder.Build();
        var step = new BakePermissionsStepType();
        step.Initialize(host, null);
        var context = new AuthenticationContext { Principal = new Principal { Id = userId, TenantId = Tenant, OrganizationId = organizationId } };

        var result = await step.Execute(context, tenants.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        return (result.Value.ShouldBeOfType<StepOutcome.Contributed>(), permissions, userId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ATokenForADepartmentAsksThePermissionServiceForItAndCarriesTheOrgIdClaim()
    {
        var (outcome, permissions, userId) = await Bake(Finance);

        outcome.Contribution.Claims.ShouldContain(claim => claim.Type == ClaimDefinitions.orgId.Name && claim.Value == Finance.ToString());
        outcome.Contribution.Claims.ShouldContain(claim => claim.Type == ClaimDefinitions.perm.Name && claim.Value == "organizations:read");
        permissions.Verify(p => p.Get(userId.ToString(), Tenant, Finance, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ATokenForNoDepartmentCarriesNoOrgIdClaimAndAsksTheServiceForNone()
    {
        var (outcome, permissions, userId) = await Bake(null);

        outcome.Contribution.Claims.ShouldNotContain(claim => claim.Type == ClaimDefinitions.orgId.Name);
        permissions.Verify(p => p.Get(userId.ToString(), Tenant, null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
