using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using FdwReferenceMultitenancy.Sql.Models;
using FdwReferenceMultitenancy.Sql.Results;
using FdwReferenceMultitenancy.Sql.Services;

namespace FdwReferenceMultitenancy.Sql.Tests;

/// <summary>
/// FDW-2891: the one provider read that lists deleted tenants asks the store for rows marked deleted, and a store failure is a failure
/// with its own code, never a shorter list.
/// </summary>
public sealed class SqlTenantProviderDeletedTenantsTests
{
    private static SqlTenantProvider Provider(Mock<IConfigurationGateway> gateway)
    {
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(g => g.Get("PlatformConfiguration")).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));
        return new SqlTenantProvider(gateways.Object, new SqlTenantConfiguration
        {
            DataStoreName = "PlatformConfiguration", PathName = "tenant", TenantsTableName = "Tenants",
            TenantFeaturesTableName = "TenantFeatures", TenantRolesTableName = "TenantRoles",
        });
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeletedTenantsAreReadByAskingForRowsMarkedDeleted()
    {
        var id = Guid.NewGuid();
        IDataCommand? asked = null;
        var gateway = new Mock<IConfigurationGateway>();
        gateway.Setup(g => g.Execute<IEnumerable<SqlTenantEntity>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback((IDataCommand command, DataStoreTarget _, CancellationToken _) => asked = command)
            .ReturnsAsync(GenericResult<IEnumerable<SqlTenantEntity>>.Success([new SqlTenantEntity { Id = id, DisplayName = "Gone", Slug = "gone" }]));
        gateway.Setup(g => g.Execute<IEnumerable<TenantFeatureEntity>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<TenantFeatureEntity>>.Success([]));
        gateway.Setup(g => g.Execute<IEnumerable<TenantRoleEntity>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<TenantRoleEntity>>.Success([]));

        var result = await Provider(gateway).GetDeletedTenants(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldHaveSingleItem().Id.ShouldBe(id);
        var filter = asked.ShouldBeAssignableTo<IQueryCommand>()!.Filter!.Root.ShouldBeAssignableTo<IFilterCondition>()!;
        filter.PropertyName.ShouldBe("IsDeleted");
        filter.Value.ShouldBe(true);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task AFailedReadOfDeletedTenantsIsAFailureWithItsOwnCode()
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway.Setup(g => g.Execute<IEnumerable<SqlTenantEntity>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<SqlTenantEntity>>.Failure(SqlTenantResultCodes.ByName("TenantQueryFailed")));

        var result = await Provider(gateway).GetDeletedTenants(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == SqlTenantResultCodes.ByName("DeletedTenantsQueryFailed").Code);
    }
}
