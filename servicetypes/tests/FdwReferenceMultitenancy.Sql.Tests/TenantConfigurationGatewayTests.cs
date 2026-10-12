using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using FdwReferenceMultitenancy.Sql.Models;
using FdwReferenceMultitenancy.Sql.Results;
using FdwReferenceMultitenancy.Sql.Services;

namespace FdwReferenceMultitenancy.Sql.Tests;

public sealed class TenantConfigurationGatewayTests
{
    [Fact]
    public async Task TenantAndRelatedRowsUseConfiguredConfigurationGateway()
    {
        var id = Guid.NewGuid();
        var gateway = new Mock<IConfigurationGateway>(MockBehavior.Strict);
        gateway.Setup(g => g.Execute<IEnumerable<SqlTenantEntity>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<SqlTenantEntity>>.Success([new SqlTenantEntity { Id = id, DisplayName = "Home", Slug = "home", IsGlobal = true }]));
        gateway.Setup(g => g.Execute<IEnumerable<TenantFeatureEntity>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<TenantFeatureEntity>>.Success([]));
        gateway.Setup(g => g.Execute<IEnumerable<TenantRoleEntity>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<TenantRoleEntity>>.Success([]));
        var gateways = new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict);
        gateways.Setup(g => g.Get("PlatformConfiguration")).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));
        var provider = new SqlTenantProvider(gateways.Object, new SqlTenantConfiguration
        {
            DataStoreName = "PlatformConfiguration", PathName = "tenant", TenantsTableName = "Tenants",
            TenantFeaturesTableName = "TenantFeatures", TenantRolesTableName = "TenantRoles",
        });

        var result = await provider.GetTenant(id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Id.ShouldBe(id);
        result.Value.IsGlobal.ShouldBeTrue();
        gateways.Verify(g => g.Get("PlatformConfiguration"), Times.Exactly(3));
        gateway.VerifyAll();
    }

    [Fact]
    public async Task MissingConfigurationGatewayReturnsFailure()
    {
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(g => g.Get("PlatformConfiguration")).Returns(
            GenericResult<IConfigurationGateway>.Failure(SqlTenantResultCodes.ByName("TenantQueryFailed")));
        var provider = new SqlTenantProvider(gateways.Object, new SqlTenantConfiguration
        {
            DataStoreName = "PlatformConfiguration", PathName = "tenant", TenantsTableName = "Tenants",
        });

        var result = await provider.GetTenant(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task RelatedQueryFailureDoesNotReturnPartialTenant(bool featuresFail, bool listTenants)
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway.Setup(g => g.Execute<IEnumerable<SqlTenantEntity>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<SqlTenantEntity>>.Success([new SqlTenantEntity { Id = Guid.NewGuid() }]));
        gateway.Setup(g => g.Execute<IEnumerable<TenantFeatureEntity>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(featuresFail
                ? GenericResult<IEnumerable<TenantFeatureEntity>>.Failure(SqlTenantResultCodes.ByName("TenantQueryFailed"))
                : GenericResult<IEnumerable<TenantFeatureEntity>>.Success([]));
        gateway.Setup(g => g.Execute<IEnumerable<TenantRoleEntity>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<TenantRoleEntity>>.Failure(SqlTenantResultCodes.ByName("TenantQueryFailed")));
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(g => g.Get("PlatformConfiguration")).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));
        var provider = new SqlTenantProvider(gateways.Object, new SqlTenantConfiguration
        {
            DataStoreName = "PlatformConfiguration", PathName = "tenant", TenantsTableName = "Tenants",
            TenantFeaturesTableName = "TenantFeatures", TenantRolesTableName = "TenantRoles",
        });

        if (listTenants)
            (await provider.GetAllTenants(TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
        else
            (await provider.GetTenant(Guid.NewGuid(), TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OrganizationReadsUseConfigurationGatewayAndPropagateFailure(bool getDefault)
    {
        var gateways = new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict);
        gateways.Setup(g => g.Get("PlatformConfiguration")).Returns(
            GenericResult<IConfigurationGateway>.Failure(SqlTenantResultCodes.ByName("TenantQueryFailed")));
        var provider = new SqlOrganizationProvider(gateways.Object, new SqlTenantConfiguration
        {
            DataStoreName = "PlatformConfiguration", PathName = "tenant",
        });

        var result = getDefault
            ? await provider.GetDefault(Guid.NewGuid(), TestContext.Current.CancellationToken)
            : await provider.Get(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        gateways.VerifyAll();
    }
}