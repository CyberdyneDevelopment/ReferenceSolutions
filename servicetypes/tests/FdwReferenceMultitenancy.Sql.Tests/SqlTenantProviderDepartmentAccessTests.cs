using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Data.Abstractions;
using FdwReferenceMultitenancy.Sql.Models;
using FdwReferenceMultitenancy.Sql.Services;

namespace FdwReferenceMultitenancy.Sql.Tests;

/// <summary>
/// Tests for the SQL tenant provider's answer to "may this person act in this department" (FDW-1802): the department exists, is the tenant's
/// and is active, and the person holds access rows in it; a host without an organization access provider fails loud.
/// </summary>
public sealed class SqlTenantProviderDepartmentAccessTests
{
    private static readonly Guid Tenant = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid OtherTenant = new("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");
    private static readonly Guid User = new("55555555-0000-0000-0000-000000000001");

    private static SqlTenantProvider Provider(Guid organizationTenant, bool active, int accessRows, bool withAccessProvider = true)
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway.Setup(g => g.Execute<IEnumerable<SqlOrganizationEntity>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<SqlOrganizationEntity>>.Success(
                [new SqlOrganizationEntity { Id = Finance, TenantId = organizationTenant, Name = "Finance", Slug = "finance", IsActive = active }]));
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(g => g.Get("PlatformConfiguration")).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));
        var access = new Mock<IOrgAccessProvider>();
        access.Setup(a => a.Get(User, Tenant, Finance, It.IsAny<CancellationToken>())).ReturnsAsync(
            GenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>.Success([.. Enumerable.Range(0, accessRows).Select(_ => new TenantOrgAccessConfiguration())]));
        return new SqlTenantProvider(
            gateways.Object,
            new SqlTenantConfiguration { DataStoreName = "PlatformConfiguration", PathName = "tenant", TenantsTableName = "Tenants" },
            null,
            withAccessProvider ? access.Object : null);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task APersonWithAccessInAnActiveDepartmentOfTheTenantMayActInIt()
    {
        var may = await Provider(Tenant, true, 1).ValidateOrganizationAccess(Tenant, Finance, User.ToString(), TestContext.Current.CancellationToken);

        may.IsSuccess.ShouldBeTrue();
        may.Value.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task APersonWithNoAccessRowsInTheDepartmentMayNot()
    {
        var may = await Provider(Tenant, true, 0).ValidateOrganizationAccess(Tenant, Finance, User.ToString(), TestContext.Current.CancellationToken);

        may.Value.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ADepartmentOfAnotherTenantMayNotBeActedInEvenWithAccessRows()
    {
        var may = await Provider(OtherTenant, true, 1).ValidateOrganizationAccess(Tenant, Finance, User.ToString(), TestContext.Current.CancellationToken);

        may.Value.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ADisabledDepartmentMayNotBeActedIn()
    {
        var may = await Provider(Tenant, false, 1).ValidateOrganizationAccess(Tenant, Finance, User.ToString(), TestContext.Current.CancellationToken);

        may.Value.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AHostWithoutAnOrganizationAccessProviderFailsLoudInsteadOfAnswering()
    {
        var may = await Provider(Tenant, true, 1, withAccessProvider: false).ValidateOrganizationAccess(Tenant, Finance, User.ToString(), TestContext.Current.CancellationToken);

        may.IsFailure.ShouldBeTrue();
    }
}
