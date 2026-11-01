using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FdwReferenceAuthentication.BakePermissions.Registration.Tests;

public sealed class BakePermissionsStepTypeTests
{
    [Fact]
    public async Task PermissionBakingReadsTenantAsSystemAndRestoresContextOnCancellation()
    {
        var previous = Mock.Of<IAuthenticationContext>();
        var accessor = new AuthenticationContextAccessor { Current = previous };
        var tenantId = Guid.NewGuid();
        var tenants = new Mock<ITenantProvider>();
        tenants.Setup(p => p.GetTenant(tenantId, It.IsAny<CancellationToken>()))
            .Returns(async (Guid _, CancellationToken _) =>
            {
                await Task.Yield();
                accessor.Current.ShouldNotBeNull().IsSystemContext.ShouldBeTrue();
                throw new OperationCanceledException();
            });
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<IAuthenticationContextAccessor>(accessor);
        builder.Services.AddSingleton(Mock.Of<IUserPermissions>());
        using var host = builder.Build();
        var step = new BakePermissionsStepType();
        step.Initialize(host, null);
        var context = new AuthenticationContext
        {
            Principal = new Principal { Id = Guid.NewGuid(), TenantId = tenantId },
        };
        await Should.ThrowAsync<OperationCanceledException>(
            () => step.Execute(context, tenants.Object, TestContext.Current.CancellationToken));
        accessor.Current.ShouldBeSameAs(previous);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PermissionBakingStopsWhenTenantLookupFailsOrReturnsNoTenant(bool missingTenant)
    {
        var tenants = new Mock<ITenantProvider>();
        tenants.Setup(p => p.GetTenant(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(missingTenant ? GenericResult<ITenant>.Success(null!)
                : GenericResult<ITenant>.Failure(Mock.Of<Fdw.Messages.IGenericMessage>()));
        var permissions = new Mock<IUserPermissions>(MockBehavior.Strict);
        var accessor = new AuthenticationContextAccessor();
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<IAuthenticationContextAccessor>(accessor);
        builder.Services.AddSingleton(tenants.Object);
        builder.Services.AddSingleton(permissions.Object);
        using var host = builder.Build();
        var step = new BakePermissionsStepType();
        step.Initialize(host, null);
        var context = new AuthenticationContext
        {
            Principal = new Principal { Id = Guid.NewGuid(), TenantId = Guid.NewGuid() },
        };

        var result = await step.Execute(context, tenants.Object, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        permissions.VerifyNoOtherCalls();
        accessor.Current.ShouldBeNull();
    }
}
