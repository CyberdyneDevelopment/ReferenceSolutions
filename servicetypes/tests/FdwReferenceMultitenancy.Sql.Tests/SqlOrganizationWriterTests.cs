using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using FdwReferenceMultitenancy.Sql.Models;
using FdwReferenceMultitenancy.Sql.Services;

namespace FdwReferenceMultitenancy.Sql.Tests;

/// <summary>
/// Tests for <see cref="SqlOrganizationWriter"/> (FDW-1758): an organization is made once with a slug no other organization of its tenant
/// has, in two writes (the domain row and the row that points at it), a failed second write removes the first, a change keeps what it
/// does not state, and the default organization cannot be disabled or removed.
/// </summary>
public sealed class SqlOrganizationWriterTests
{
    private static readonly Guid Tenant = new("11111111-0000-0000-0000-000000000001");
    private static readonly Guid Legal = new("44444444-0000-0000-0000-000000000009");
    private static readonly Guid Default = new("44444444-0000-0000-0000-000000000001");

    private static Mock<IOrganizationProvider> Organizations()
    {
        var organizations = new Mock<IOrganizationProvider>();
        organizations.Setup(o => o.Get(Legal, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<OrganizationConfiguration>.Success(
            new OrganizationConfiguration { Id = Legal, TenantId = Tenant, Name = "Legal", Slug = "legal", IsActive = true }));
        organizations.Setup(o => o.Get(Default, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<OrganizationConfiguration>.Success(
            new OrganizationConfiguration { Id = Default, TenantId = Tenant, Name = "Default Organization", Slug = "default", IsActive = true, IsDefault = true }));
        organizations.Setup(o => o.Get(It.Is<Guid>(id => id != Legal && id != Default), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Failure(new Fdw.Messages.GenericMessage("not found")));
        return organizations;
    }

    private static (SqlOrganizationWriter Writer, Mock<IConfigurationGateway> Gateway) Writer(
        bool tenantExists = true, string[]? existingSlugs = null, Func<int, int>? writeRows = null)
    {
        var written = 0;
        var gateway = new Mock<IConfigurationGateway>();
        gateway.Setup(g => g.Execute<IEnumerable<SqlTenantRow>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<SqlTenantRow>>.Success(tenantExists ? [new SqlTenantRow { RowId = 3, Id = Tenant }] : []));
        gateway.Setup(g => g.Execute<IEnumerable<SqlOrganizationEntity>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<SqlOrganizationEntity>>.Success([.. (existingSlugs ?? []).Select(slug => new SqlOrganizationEntity { Slug = slug })]));
        gateway.Setup(g => g.Execute<IEnumerable<SqlOrganizationRow>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<SqlOrganizationRow>>.Success([new SqlOrganizationRow { RowId = 7, Id = Legal }]));
        gateway.Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => GenericResult<int>.Success((writeRows ?? (_ => 1))(++written)));
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(g => g.Get("PlatformConfiguration")).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));
        var writer = new SqlOrganizationWriter(gateways.Object, Organizations().Object, new SqlTenantConfiguration
        {
            DataStoreName = "PlatformConfiguration", PathName = "tenant", TenantsTableName = "Tenants",
        });
        return (writer, gateway);
    }

    private static void Writes(Mock<IConfigurationGateway> gateway, Times times) =>
        gateway.Verify(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), times);

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnOrganizationIsMadeWithAFreshIdAndTwoWrites()
    {
        var (writer, gateway) = Writer();

        var made = await writer.Create(new NewOrganization(Tenant, "Legal", "legal"), TestContext.Current.CancellationToken);

        made.IsSuccess.ShouldBeTrue();
        made.Value.ShouldNotBe(Guid.Empty);
        Writes(gateway, Times.Exactly(2));
    }

    [Theory]
    [InlineData("legal")]
    [InlineData("LEGAL")]
    [Trait("Priority", "P0")]
    public async Task ASlugAnotherOrganizationOfTheTenantHasMakesNothing(string slug)
    {
        var (writer, gateway) = Writer(existingSlugs: ["default", "legal"]);

        var made = await writer.Create(new NewOrganization(Tenant, "Legal Two", slug), TestContext.Current.CancellationToken);

        made.IsFailure.ShouldBeTrue();
        Writes(gateway, Times.Never());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ATenantThatDoesNotExistMakesNothing()
    {
        var (writer, gateway) = Writer(tenantExists: false);

        var made = await writer.Create(new NewOrganization(Tenant, "Legal", "legal"), TestContext.Current.CancellationToken);

        made.IsFailure.ShouldBeTrue();
        Writes(gateway, Times.Never());
    }

    [Theory]
    [InlineData("", "legal")]
    [InlineData("Legal", " ")]
    [Trait("Priority", "P0")]
    public async Task ANameAndASlugAreRequired(string name, string slug)
    {
        var (writer, gateway) = Writer();

        var made = await writer.Create(new NewOrganization(Tenant, name, slug), TestContext.Current.CancellationToken);

        made.IsFailure.ShouldBeTrue();
        Writes(gateway, Times.Never());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedSecondWriteRemovesTheFirstAgain()
    {
        var (writer, gateway) = Writer(writeRows: call => call == 2 ? 0 : 1);

        var made = await writer.Create(new NewOrganization(Tenant, "Legal", "legal"), TestContext.Current.CancellationToken);

        made.IsFailure.ShouldBeTrue();
        // the domain row, the failed link, then the removal of the link (none) and of the domain row
        Writes(gateway, Times.Exactly(4));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ARenameWritesOnceAndKeepsWhatItDoesNotState()
    {
        var (writer, gateway) = Writer();

        var changed = await writer.Update(Legal, new OrganizationChange("Legal Affairs", null), TestContext.Current.CancellationToken);

        changed.IsSuccess.ShouldBeTrue();
        Writes(gateway, Times.Once());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task TheDefaultOrganizationCannotBeDisabledButCanBeRenamed()
    {
        var (writer, gateway) = Writer();

        var disabled = await writer.Update(Default, new OrganizationChange(null, false), TestContext.Current.CancellationToken);
        var renamed = await writer.Update(Default, new OrganizationChange("Home", null), TestContext.Current.CancellationToken);

        disabled.IsFailure.ShouldBeTrue();
        renamed.IsSuccess.ShouldBeTrue();
        Writes(gateway, Times.Once());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnOrganizationThatDoesNotExistCannotBeChangedOrRemoved()
    {
        var (writer, gateway) = Writer();

        var changed = await writer.Update(Guid.NewGuid(), new OrganizationChange("X", null), TestContext.Current.CancellationToken);
        var removed = await writer.Delete(Guid.NewGuid(), TestContext.Current.CancellationToken);

        changed.IsFailure.ShouldBeTrue();
        removed.IsFailure.ShouldBeTrue();
        Writes(gateway, Times.Never());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task TheDefaultOrganizationCannotBeRemoved()
    {
        var (writer, gateway) = Writer();

        var removed = await writer.Delete(Default, TestContext.Current.CancellationToken);

        removed.IsFailure.ShouldBeTrue();
        Writes(gateway, Times.Never());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task RemovingAnOrganizationDeletesItsImplementationRowThenItsDomainRow()
    {
        var (writer, gateway) = Writer();

        var removed = await writer.Delete(Legal, TestContext.Current.CancellationToken);

        removed.IsSuccess.ShouldBeTrue();
        Writes(gateway, Times.Exactly(2));
    }
}
