using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using FdwReferenceMultitenancy.Sql.Models;
using FdwReferenceMultitenancy.Sql.Services;

namespace FdwReferenceMultitenancy.Sql.Tests;

/// <summary>
/// Tests for deleting and restoring a company in <see cref="SqlTenantWriter"/> (FDW-1759): a disabled company is marked deleted with one
/// write and nothing it holds is touched, the global company and an active one are refused, a missing one is not found, and a company
/// already in the state asked for writes nothing.
/// </summary>
public sealed class SqlTenantDeleteTests
{
    private static readonly Guid Acme = new("11111111-0000-0000-0000-000000000002");
    private static readonly Guid Home = new("11111111-0000-0000-0000-000000000001");

    private static (SqlTenantWriter Writer, Mock<IConfigurationGateway> Gateway) Writer(bool isActive, bool isDeleted, bool isGlobal = false, bool exists = true)
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway.Setup(g => g.Execute<IEnumerable<SqlTenantState>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<SqlTenantState>>.Success(
                exists ? [new SqlTenantState { Id = isGlobal ? Home : Acme, IsActive = isActive, IsDeleted = isDeleted, IsGlobal = isGlobal }] : []));
        gateway.Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Success(1));
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(g => g.Get("PlatformConfiguration")).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));
        var writer = new SqlTenantWriter(gateways.Object, Mock.Of<ITenantProvider>(), new SqlTenantConfiguration
        {
            DataStoreName = "PlatformConfiguration", PathName = "tenant", TenantsTableName = "Tenants",
        });
        return (writer, gateway);
    }

    private static void Writes(Mock<IConfigurationGateway> gateway, Times times) =>
        gateway.Verify(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), times);

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ADisabledCompanyIsMarkedDeletedWithOneWrite()
    {
        var (writer, gateway) = Writer(isActive: false, isDeleted: false);

        var deleted = await writer.SetDeleted(Acme, true, TestContext.Current.CancellationToken);

        deleted.IsSuccess.ShouldBeTrue();
        Writes(gateway, Times.Once());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnActiveCompanyCannotBeDeleted()
    {
        var (writer, gateway) = Writer(isActive: true, isDeleted: false);

        var deleted = await writer.SetDeleted(Acme, true, TestContext.Current.CancellationToken);

        deleted.IsFailure.ShouldBeTrue();
        Writes(gateway, Times.Never());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task TheGlobalCompanyCanNeitherBeDeletedNorRestored()
    {
        var (writer, gateway) = Writer(isActive: false, isDeleted: false, isGlobal: true);

        var deleted = await writer.SetDeleted(Home, true, TestContext.Current.CancellationToken);
        var restored = await writer.SetDeleted(Home, false, TestContext.Current.CancellationToken);

        deleted.IsFailure.ShouldBeTrue();
        restored.IsFailure.ShouldBeTrue();
        Writes(gateway, Times.Never());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ADeletedCompanyIsRestoredWithOneWrite()
    {
        var (writer, gateway) = Writer(isActive: false, isDeleted: true);

        var restored = await writer.SetDeleted(Acme, false, TestContext.Current.CancellationToken);

        restored.IsSuccess.ShouldBeTrue();
        Writes(gateway, Times.Once());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ACompanyAlreadyInTheStateAskedForWritesNothing()
    {
        var (deleted, deletedGateway) = Writer(isActive: false, isDeleted: true);
        var (live, liveGateway) = Writer(isActive: false, isDeleted: false);

        var again = await deleted.SetDeleted(Acme, true, TestContext.Current.CancellationToken);
        var unneeded = await live.SetDeleted(Acme, false, TestContext.Current.CancellationToken);

        again.IsSuccess.ShouldBeTrue();
        unneeded.IsSuccess.ShouldBeTrue();
        Writes(deletedGateway, Times.Never());
        Writes(liveGateway, Times.Never());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ACompanyThatDoesNotExistIsNotFound()
    {
        var (writer, gateway) = Writer(isActive: false, isDeleted: false, exists: false);

        var deleted = await writer.SetDeleted(Acme, true, TestContext.Current.CancellationToken);

        deleted.IsFailure.ShouldBeTrue();
        Writes(gateway, Times.Never());
    }
}
