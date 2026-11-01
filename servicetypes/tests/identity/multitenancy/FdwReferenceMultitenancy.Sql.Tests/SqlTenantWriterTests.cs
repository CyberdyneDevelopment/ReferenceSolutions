using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using FdwReferenceMultitenancy.Sql.Results;
using FdwReferenceMultitenancy.Sql.Services;

namespace FdwReferenceMultitenancy.Sql.Tests;

/// <summary>
/// Tests for <see cref="SqlTenantWriter"/> (FDW-1578): a tenant is made once with a slug no other has, a change keeps what it does not
/// state, the global tenant cannot be disabled or removed, and a write that touches no row is a failure.
/// </summary>
public sealed class SqlTenantWriterTests
{
    private static readonly Guid Home = new("11111111-0000-0000-0000-000000000001");
    private static readonly Guid Acme = new("11111111-0000-0000-0000-000000000002");

    private static ITenant Tenant(Guid id, string slug, string name, bool isActive, bool isGlobal, string? connection)
    {
        var tenant = new Mock<ITenant>();
        tenant.SetupGet(t => t.Id).Returns(id);
        tenant.SetupGet(t => t.Slug).Returns(slug);
        tenant.SetupGet(t => t.DisplayName).Returns(name);
        tenant.SetupGet(t => t.IsActive).Returns(isActive);
        tenant.SetupGet(t => t.IsGlobal).Returns(isGlobal);
        tenant.SetupGet(t => t.ConnectionName).Returns(connection);
        return tenant.Object;
    }

    private static Mock<ITenantProvider> Tenants()
    {
        var home = Tenant(Home, "home", "Home", true, true, null);
        var acme = Tenant(Acme, "acme", "Acme", true, false, "AcmeDb");
        var tenants = new Mock<ITenantProvider>();
        tenants.Setup(t => t.GetAllTenants(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<ITenant>>.Success([home, acme]));
        tenants.Setup(t => t.GetTenant(Home, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<ITenant>.Success(home));
        tenants.Setup(t => t.GetTenant(Acme, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<ITenant>.Success(acme));
        tenants.Setup(t => t.GetTenant(It.Is<Guid>(id => id != Home && id != Acme), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ITenant>.Failure(SqlTenantResultCodes.ByName("TenantNotFound")));
        return tenants;
    }

    private static (SqlTenantWriter Writer, Mock<IConfigurationGateway> Gateway) Writer(int rowsWritten = 1)
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway.Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Success(rowsWritten));
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(g => g.Get("PlatformConfiguration")).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));
        var writer = new SqlTenantWriter(gateways.Object, Tenants().Object, new SqlTenantConfiguration
        {
            DataStoreName = "PlatformConfiguration", PathName = "tenant", TenantsTableName = "Tenants",
        });
        return (writer, gateway);
    }

    private static void NothingWritten(Mock<IConfigurationGateway> gateway) =>
        gateway.Verify(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), Times.Never);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ATenantIsMadeWithAFreshIdAndOneWrite()
    {
        var (writer, gateway) = Writer();

        var made = await writer.Create(new NewTenant("Beta", "beta", null, null), TestContext.Current.CancellationToken);

        made.IsSuccess.ShouldBeTrue();
        made.Value.ShouldNotBe(Guid.Empty);
        gateway.Verify(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("acme")]
    [InlineData("ACME")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ASlugAnotherTenantHasIsRefusedAndNothingIsWritten(string slug)
    {
        var (writer, gateway) = Writer();

        var made = await writer.Create(new NewTenant("Another", slug, null, null), TestContext.Current.CancellationToken);

        made.IsFailure.ShouldBeTrue();
        NothingWritten(gateway);
    }

    [Theory]
    [InlineData("", "slug")]
    [InlineData("  ", "slug")]
    [InlineData("Name", "")]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ATenantWithNoNameOrNoSlugIsRefused(string name, string slug)
    {
        var (writer, gateway) = Writer();

        var made = await writer.Create(new NewTenant(name, slug, null, null), TestContext.Current.CancellationToken);

        made.IsFailure.ShouldBeTrue();
        NothingWritten(gateway);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARenameWritesOnceAndAChangeToAMissingTenantWritesNothing()
    {
        var (writer, gateway) = Writer();

        var renamed = await writer.Update(Acme, new TenantChange("Acme Corp", null, null), TestContext.Current.CancellationToken);
        var missing = await writer.Update(Guid.NewGuid(), new TenantChange("Nobody", null, null), TestContext.Current.CancellationToken);

        renamed.IsSuccess.ShouldBeTrue();
        missing.IsFailure.ShouldBeTrue();
        gateway.Verify(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task TheGlobalTenantCannotBeDisabledButCanBeRenamedOrKeptActive()
    {
        var (writer, gateway) = Writer();

        var disabled = await writer.Update(Home, new TenantChange(null, false, null), TestContext.Current.CancellationToken);
        NothingWritten(gateway);
        var renamed = await writer.Update(Home, new TenantChange("Headquarters", null, null), TestContext.Current.CancellationToken);
        var kept = await writer.Update(Home, new TenantChange(null, true, null), TestContext.Current.CancellationToken);

        disabled.IsFailure.ShouldBeTrue();
        renamed.IsSuccess.ShouldBeTrue();
        kept.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnEmptyNewNameIsRefusedAndNothingIsWritten()
    {
        var (writer, gateway) = Writer();

        var renamed = await writer.Update(Acme, new TenantChange(" ", null, null), TestContext.Current.CancellationToken);

        renamed.IsFailure.ShouldBeTrue();
        NothingWritten(gateway);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task TheGlobalTenantCannotBeDeletedAndAnotherTenantCanBe()
    {
        var (writer, gateway) = Writer();

        var global = await writer.Delete(Home, TestContext.Current.CancellationToken);
        NothingWritten(gateway);
        var other = await writer.Delete(Acme, TestContext.Current.CancellationToken);

        global.IsFailure.ShouldBeTrue();
        other.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AWriteThatTouchesNoRowIsAFailure()
    {
        var (writer, _) = Writer(rowsWritten: 0);

        var renamed = await writer.Update(Acme, new TenantChange("Acme Corp", null, null), TestContext.Current.CancellationToken);

        renamed.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AMissingTenantStoreConfigurationFailsLoudAndWritesNothing()
    {
        var gateways = new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict);
        var writer = new SqlTenantWriter(gateways.Object, Tenants().Object, new SqlTenantConfiguration { DataStoreName = "PlatformConfiguration" });

        var renamed = await writer.Update(Acme, new TenantChange("Acme Corp", null, null), TestContext.Current.CancellationToken);

        renamed.IsFailure.ShouldBeTrue();
    }
}
